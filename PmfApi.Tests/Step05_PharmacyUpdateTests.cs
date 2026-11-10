using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PmfApi.Api.Authorization;
using PmfApi.Api.Controllers;
using PmfApi.Application.Dtos;
using PmfApi.Application.Exceptions;
using PmfApi.Application.Interfaces;
using PmfApi.Domain.Entities;
using PmfApi.Infrastructure.Persistence;
using PmfApi.Infrastructure.Persistence.Services;
using PmfApi.Tests.Support;

namespace PmfApi.Tests;

// ─────────────────────────────────────────────────────────────────────────────────────────────────
// Step 5 regression: a partial pharmacy update must never null out (or zero out) omitted fields.
//
// The bug being guarded: when the update DTO bound omitted JSON properties to non-nullable defaults
// (0 for ints, "" for strings) and the service copied every property onto the entity, a request that
// only carried { "name": "X" } silently wrote PhoneNumber = 0, FreshnessThreshold = 0, and so on.
// It can come back through three different doors, so there is a test at each one:
//   1. the DTO's shape        (PharmacyUpdateRequestShapeTests)
//   2. the service's copying  (PharmacyUpdateServiceTests)
//   3. the real HTTP binding  (PharmacyUpdateEndpointTests, needs PostgreSQL)
// plus the controller's authorization behaviour (PharmacyUpdateControllerTests).
// ─────────────────────────────────────────────────────────────────────────────────────────────────

public class PharmacyUpdateRequestShapeTests
{
    [Fact]
    public void Every_field_is_nullable_so_that_omitted_can_mean_unchanged()
    {
        // If someone adds `public int Foo { get; init; }` the binder fills it with 0 when the client omits
        // it, and the service can no longer tell "not sent" from "set to zero". Failing here is the
        // early warning, before any data is damaged.
        var nullability = new NullabilityInfoContext();
        var offenders = typeof(PharmacyUpdateRequest)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => nullability.Create(p).ReadState != NullabilityState.Nullable)
            .Select(p => p.Name)
            .ToList();

        Assert.True(offenders.Count == 0,
            "These PharmacyUpdateRequest properties are not nullable, so an omitted value cannot be told " +
            $"apart from a real one: {string.Join(", ", offenders)}");
    }

    [Fact]
    public void A_body_with_only_name_leaves_every_other_field_null()
    {
        var request = JsonSerializer.Deserialize<PharmacyUpdateRequest>("""{"name":"Renamed"}""", Json.Options)!;

        Assert.Equal("Renamed", request.Name);
        Assert.Null(request.LicenseNumber);
        Assert.Null(request.LocationId);
        Assert.Null(request.PhoneNumber);
        Assert.Null(request.Email);
        Assert.Null(request.FreshnessThreshold);
    }

    [Fact]
    public void An_empty_email_string_is_preserved_as_empty_not_collapsed_to_null()
    {
        // The documented way to clear the email is to send "". It must stay distinguishable from omission.
        var request = JsonSerializer.Deserialize<PharmacyUpdateRequest>("""{"email":""}""", Json.Options)!;

        Assert.Equal("", request.Email);
    }
}

public class PharmacyUpdateServiceTests
{
    private const int PharmacyId = 1;
    private const int OtherPharmacyId = 2;

    private sealed record Snapshot(
        string Name, string LicenceNumber, int LocationId, int PhoneNumber, string? Email, int FreshnessThreshold);

    private readonly string _dbName = Guid.NewGuid().ToString();

    private static PharmacyAdminService NewService(PmfDbContext db) =>
        new(db, Substitute.For<ILogger<PharmacyAdminService>>());

    private async Task SeedAsync()
    {
        await using var db = TestDb.Create(_dbName);
        db.Pharmacies.AddRange(
            new Pharmacy
            {
                Id = PharmacyId,
                Name = "Original Pharmacy",
                LicenceNumber = "LIC-001",
                PhoneNumber = 911223344,
                Email = "orig@pharmacy.test",
                LocationId = 7,
                FreshnessThreshold = 12,
                IsVerified = true,
                ReliablityScore = 4.5m,
                LastInventoryUpdateAt = DateTime.UtcNow,
            },
            new Pharmacy
            {
                Id = OtherPharmacyId,
                Name = "Other Pharmacy",
                LicenceNumber = "LIC-002",
                PhoneNumber = 922334455,
                LocationId = 7,
                FreshnessThreshold = 24,
                LastInventoryUpdateAt = DateTime.UtcNow,
            });
        await db.SaveChangesAsync();
    }

    private async Task<Snapshot> ReadAsync(int id = PharmacyId)
    {
        // A fresh context over the same in-memory store: proves what was persisted, not what is tracked.
        await using var db = TestDb.Create(_dbName);
        var p = await db.Pharmacies.AsNoTracking().SingleAsync(x => x.Id == id);
        return new Snapshot(p.Name, p.LicenceNumber, p.LocationId, p.PhoneNumber, p.Email, p.FreshnessThreshold);
    }

    /// <summary>
    /// Applies the request and asserts the stored row equals the "before" row with exactly the expected
    /// change applied and nothing else.
    /// </summary>
    private async Task AssertOnlyChangesAsync(PharmacyUpdateRequest request, Func<Snapshot, Snapshot> expectedChange)
    {
        await SeedAsync();
        var before = await ReadAsync();

        await using (var db = TestDb.Create(_dbName))
        {
            var result = await NewService(db).UpdateAsync(PharmacyId, request, CancellationToken.None);
            Assert.NotNull(result);
        }

        Assert.Equal(expectedChange(before), await ReadAsync());
    }

    [Fact]
    public Task Sending_only_the_name_changes_only_the_name() =>
        AssertOnlyChangesAsync(
            new PharmacyUpdateRequest { Name = "Renamed" },
            s => s with { Name = "Renamed" });

    [Fact]
    public Task Sending_only_the_licence_changes_only_the_licence() =>
        AssertOnlyChangesAsync(
            new PharmacyUpdateRequest { LicenseNumber = "LIC-900" },
            s => s with { LicenceNumber = "LIC-900" });

    [Fact]
    public Task Sending_only_the_location_changes_only_the_location() =>
        AssertOnlyChangesAsync(
            new PharmacyUpdateRequest { LocationId = 9 },
            s => s with { LocationId = 9 });

    [Fact]
    public Task Sending_only_the_phone_changes_only_the_phone() =>
        AssertOnlyChangesAsync(
            new PharmacyUpdateRequest { PhoneNumber = 955667788 },
            s => s with { PhoneNumber = 955667788 });

    [Fact]
    public Task Sending_only_the_email_changes_only_the_email() =>
        AssertOnlyChangesAsync(
            new PharmacyUpdateRequest { Email = "new@pharmacy.test" },
            s => s with { Email = "new@pharmacy.test" });

    [Fact]
    public Task Sending_only_the_freshness_threshold_changes_only_that() =>
        AssertOnlyChangesAsync(
            new PharmacyUpdateRequest { FreshnessThreshold = 48 },
            s => s with { FreshnessThreshold = 48 });

    [Fact]
    public Task A_zero_freshness_threshold_is_a_real_value_not_a_missing_one() =>
        // FreshnessThreshold has a valid range starting at 0, so 0 must be stored when it is sent.
        AssertOnlyChangesAsync(
            new PharmacyUpdateRequest { FreshnessThreshold = 0 },
            s => s with { FreshnessThreshold = 0 });

    [Fact]
    public Task An_empty_request_changes_nothing() =>
        AssertOnlyChangesAsync(new PharmacyUpdateRequest(), s => s);

    [Fact]
    public Task An_empty_email_string_clears_the_email_and_nothing_else() =>
        AssertOnlyChangesAsync(
            new PharmacyUpdateRequest { Email = "" },
            s => s with { Email = null });

    [Fact]
    public Task A_whitespace_name_or_licence_is_ignored_rather_than_written() =>
        AssertOnlyChangesAsync(
            new PharmacyUpdateRequest { Name = "   ", LicenseNumber = "   " },
            s => s);

    [Fact]
    public Task Several_fields_in_one_request_all_apply_and_the_rest_stay() =>
        AssertOnlyChangesAsync(
            new PharmacyUpdateRequest { Name = "Renamed", PhoneNumber = 955667788 },
            s => s with { Name = "Renamed", PhoneNumber = 955667788 });

    [Fact]
    public async Task Re_sending_the_pharmacys_own_licence_is_not_a_duplicate()
    {
        await SeedAsync();
        await using var db = TestDb.Create(_dbName);

        var result = await NewService(db).UpdateAsync(
            PharmacyId, new PharmacyUpdateRequest { LicenseNumber = "LIC-001" }, CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task A_licence_that_belongs_to_another_pharmacy_is_rejected_and_nothing_is_saved()
    {
        await SeedAsync();
        var before = await ReadAsync();

        await using (var db = TestDb.Create(_dbName))
        {
            // The name is applied to the tracked entity before the licence check throws, so this also
            // proves the half-applied change is never persisted.
            await Assert.ThrowsAsync<DuplicateLicenseException>(() => NewService(db).UpdateAsync(
                PharmacyId,
                new PharmacyUpdateRequest { Name = "Should Not Stick", LicenseNumber = "LIC-002" },
                CancellationToken.None));
        }

        Assert.Equal(before, await ReadAsync());
    }

    [Fact]
    public async Task An_unknown_pharmacy_returns_null()
    {
        await SeedAsync();
        await using var db = TestDb.Create(_dbName);

        var result = await NewService(db).UpdateAsync(
            999, new PharmacyUpdateRequest { Name = "Ghost" }, CancellationToken.None);

        Assert.Null(result);
    }
}

public class PharmacyUpdateControllerTests
{
    private const string Policy = "PharmacyOwnerOrAdmin";

    private readonly IPharmacyAdminService _admin = Substitute.For<IPharmacyAdminService>();
    private readonly IPharmaciesService _pharmacies = Substitute.For<IPharmaciesService>();
    private readonly IAuthorizationService _authorization = Substitute.For<IAuthorizationService>();

    private PharmacyAdminController NewController() => new(_admin, _pharmacies, _authorization)
    {
        ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = Principals.Create(10, RoleNames.Pharmacy) },
        },
    };

    private static Pharmacy SomePharmacy() => new() { Id = 1, Name = "P", LicenceNumber = "L" };

    private static PharmacyResponse SomeResponse() =>
        new(1, "P", "L", 1, true, 0m, 24, DateTime.UtcNow, DateTime.UtcNow);

    private void PharmacyExists() =>
        _pharmacies.GetEntityByIdAsync(1, Arg.Any<CancellationToken>()).Returns(SomePharmacy());

    private void PolicyResult(bool succeeds) =>
        _authorization.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), Policy)
            .Returns(succeeds ? AuthorizationResult.Success() : AuthorizationResult.Failed());

    [Fact]
    public async Task An_authorized_caller_reaches_the_service_with_the_request_untouched()
    {
        PharmacyExists();
        PolicyResult(true);
        var request = new PharmacyUpdateRequest { Name = "Renamed" };
        _admin.UpdateAsync(1, request, Arg.Any<CancellationToken>()).Returns(SomeResponse());

        var result = await NewController().UpdatePharmacy(1, request, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        await _admin.Received(1).UpdateAsync(1, request, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_caller_who_fails_the_policy_gets_403_and_the_service_is_never_called()
    {
        PharmacyExists();
        PolicyResult(false);

        var result = await NewController().UpdatePharmacy(1, new PharmacyUpdateRequest { Name = "X" }, CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
        await _admin.DidNotReceiveWithAnyArgs().UpdateAsync(default, default!, default);
    }

    [Fact]
    public async Task An_unknown_pharmacy_is_404_before_authorization_or_the_service_run()
    {
        _pharmacies.GetEntityByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Pharmacy?)null);

        var result = await NewController().UpdatePharmacy(1, new PharmacyUpdateRequest { Name = "X" }, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
        await _admin.DidNotReceiveWithAnyArgs().UpdateAsync(default, default!, default);
    }

    [Fact]
    public async Task A_duplicate_licence_becomes_a_409_problem_response()
    {
        PharmacyExists();
        PolicyResult(true);
        _admin.UpdateAsync(1, Arg.Any<PharmacyUpdateRequest>(), Arg.Any<CancellationToken>())
            .Returns<PharmacyResponse?>(_ => throw new DuplicateLicenseException());

        var result = await NewController().UpdatePharmacy(1, new PharmacyUpdateRequest { LicenseNumber = "DUP" }, CancellationToken.None);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, Assert.IsType<ProblemDetails>(conflict.Value).Status);
    }
}

[Collection(PostgresCollection.Name)]
public class PharmacyUpdateEndpointTests(PostgresFixture fx)
{
    private TestWorld W => fx.World;

    private static StringContent RawJson(string json) => new(json, Encoding.UTF8, "application/json");

    private async Task<Pharmacy> ReadPharmacyAsync()
    {
        await using var db = fx.NewDbContext();
        return await db.Pharmacies.AsNoTracking().SingleAsync(p => p.Id == W.PharmacyCId);
    }

    [RequiresPostgresFact]
    public async Task A_PUT_carrying_only_the_name_changes_only_the_name_over_real_HTTP_binding()
    {
        var before = await ReadPharmacyAsync();

        // Raw JSON on purpose: the point is what the MVC binder does with an omitted property.
        var response = await fx.Factory.ClientFor(W.PharmacyAdmin)
            .PutAsync(Routes.AdminPharmacy(W.PharmacyCId), RawJson("""{"name":"Renamed By Admin"}"""));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var after = await ReadPharmacyAsync();
        Assert.Equal("Renamed By Admin", after.Name);
        Assert.Equal(before.LicenceNumber, after.LicenceNumber);
        Assert.Equal(before.PhoneNumber, after.PhoneNumber);
        Assert.Equal(before.Email, after.Email);
        Assert.Equal(before.LocationId, after.LocationId);
        Assert.Equal(before.FreshnessThreshold, after.FreshnessThreshold);
    }

    [RequiresPostgresFact]
    public async Task An_explicit_JSON_null_is_treated_the_same_as_omission()
    {
        var before = await ReadPharmacyAsync();

        var response = await fx.Factory.ClientFor(W.PharmacyAdmin)
            .PutAsync(Routes.AdminPharmacy(W.PharmacyCId), RawJson("""{"phoneNumber":null,"email":null,"name":"Still Here"}"""));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var after = await ReadPharmacyAsync();
        Assert.Equal("Still Here", after.Name);
        Assert.Equal(before.PhoneNumber, after.PhoneNumber);
        Assert.Equal(before.Email, after.Email);
    }

    [RequiresPostgresFact]
    public async Task An_out_of_range_phone_is_rejected_with_400_and_nothing_changes()
    {
        var before = await ReadPharmacyAsync();

        var response = await fx.Factory.ClientFor(W.PharmacyAdmin)
            .PutAsync(Routes.AdminPharmacy(W.PharmacyCId), RawJson("""{"name":"Must Not Stick","phoneNumber":123}"""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var after = await ReadPharmacyAsync();
        Assert.Equal(before.Name, after.Name);
        Assert.Equal(before.PhoneNumber, after.PhoneNumber);
    }

    [RequiresPostgresFact]
    public async Task A_patient_cannot_update_a_pharmacy()
    {
        var response = await fx.Factory.ClientFor(W.PatientA)
            .PutAsync(Routes.AdminPharmacy(W.PharmacyCId), RawJson("""{"name":"Hijacked"}"""));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.NotEqual("Hijacked", (await ReadPharmacyAsync()).Name);
    }

    [RequiresPostgresFact]
    public async Task Staff_of_a_different_pharmacy_cannot_update_this_one()
    {
        var response = await fx.Factory.ClientFor(W.OwnerB)
            .PutAsync(Routes.AdminPharmacy(W.PharmacyCId), RawJson("""{"name":"Hijacked"}"""));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.NotEqual("Hijacked", (await ReadPharmacyAsync()).Name);
    }

    [RequiresPostgresFact]
    public async Task An_anonymous_caller_gets_401()
    {
        var response = await fx.Factory.ClientFor(null)
            .PutAsync(Routes.AdminPharmacy(W.PharmacyCId), RawJson("""{"name":"Hijacked"}"""));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
