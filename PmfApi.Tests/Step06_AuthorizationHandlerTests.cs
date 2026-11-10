using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using PmfApi.Api.Authorization;
using PmfApi.Domain.Entities;
using PmfApi.Infrastructure.Persistence;
using PmfApi.Tests.Support;
using System.Security.Claims;

namespace PmfApi.Tests;

// ─────────────────────────────────────────────────────────────────────────────────────────────────
// Step 6: the authorization handlers.
//
// A handler that is wrong in the "too strict" direction produces no error at all: the requirement is
// simply never satisfied and the caller gets a 403/404. So every matrix below pins BOTH directions,
// each row says in plain words which behaviour it protects, and every row also asserts that the handler
// did not call context.Fail(). A handler that Fail()s vetoes every other handler registered for the
// same requirement, so "not succeeded" and "failed" are different outcomes worth keeping apart.
//
// Rows marked [characterization] pin what the code does today even though it is arguable; if you change
// the policy on purpose, changing that row is the point.
// ─────────────────────────────────────────────────────────────────────────────────────────────────

internal static class Decision
{
    public static void Assert(AuthorizationHandlerContext context, bool expectedGrant, string scenario)
    {
        Xunit.Assert.True(
            context.HasSucceeded == expectedGrant,
            $"{scenario}: expected {(expectedGrant ? "GRANT" : "DENY")} but the handler " +
            $"{(context.HasSucceeded ? "granted" : "did not grant")}.");

        Xunit.Assert.False(
            context.HasFailed,
            $"{scenario}: the handler called Fail(), which would veto other handlers. Deny by returning, not by failing.");
    }
}

public class PharmacyOwnerHandlerTests
{
    private const int PharmacyA = 1;
    private const int PharmacyB = 2;

    private const int StaffOfA = 10;
    private const int InactiveStaffOfA = 11;
    private const int StaffOfB = 12;
    private const int NotStaffAnywhere = 13;

    private static PmfDbContext SeedStaff()
    {
        var db = TestDb.Create();
        db.Pharmacies.AddRange(
            new Pharmacy { Id = PharmacyA, Name = "A", LicenceNumber = "LIC-A" },
            new Pharmacy { Id = PharmacyB, Name = "B", LicenceNumber = "LIC-B" });
        db.PharmacyStaff.AddRange(
            new PharmacyStaff { Id = 1, UserId = StaffOfA, PharmacyId = PharmacyA, Position = "Pharmacist", IsActive = true },
            new PharmacyStaff { Id = 2, UserId = InactiveStaffOfA, PharmacyId = PharmacyA, Position = "Pharmacist", IsActive = false },
            new PharmacyStaff { Id = 3, UserId = StaffOfB, PharmacyId = PharmacyB, Position = "Pharmacist", IsActive = true });
        db.SaveChanges();
        return db;
    }

    // scenario, resource pharmacy, allowPlatformAdmin, requireElevatedStaffRole, user id, roles, expected grant
    public static TheoryData<string, int, bool, bool, int?, string[], bool> Matrix => new()
    {
        // ── platform admins (policy PharmacyOwnerOrAdmin) ──
        { "SysAdmin with no staff row passes the platform-admin policy", PharmacyA, true, true, NotStaffAnywhere, [RoleNames.SystemAdmin], true },
        { "PharmacyAdmin with no staff row passes the platform-admin policy", PharmacyA, true, true, NotStaffAnywhere, [RoleNames.PharmacyAdmin], true },
        { "SysAdmin passes on any pharmacy, not just one", PharmacyB, true, true, NotStaffAnywhere, [RoleNames.SystemAdmin], true },
        { "SysAdmin does NOT bypass the plain PharmacyOwner policy (no staff row, bypass off)", PharmacyA, false, false, NotStaffAnywhere, [RoleNames.SystemAdmin], false },

        // ── pharmacy owners / staff ──
        { "Pharmacy-role user with an active staff row for THIS pharmacy is granted", PharmacyA, true, true, StaffOfA, [RoleNames.Pharmacy], true },
        { "Pharmacy-role staff of the OTHER pharmacy is denied", PharmacyA, true, true, StaffOfB, [RoleNames.Pharmacy], false },
        { "The same user is granted on their own pharmacy (other direction of the line above)", PharmacyB, true, true, StaffOfB, [RoleNames.Pharmacy], true },
        { "An inactive staff row grants nothing", PharmacyA, true, true, InactiveStaffOfA, [RoleNames.Pharmacy], false },
        { "Pharmacy-role user with no staff row anywhere is denied", PharmacyA, true, true, NotStaffAnywhere, [RoleNames.Pharmacy], false },
        { "PharmacyAdmin who is also staff of this pharmacy is granted (via the admin bypass or the row)", PharmacyA, true, true, StaffOfA, [RoleNames.PharmacyAdmin], true },

        // ── the role gate (RequireElevatedStaffRole) ──
        { "A Patient who happens to have an active staff row is denied when the elevated-role gate is on", PharmacyA, true, true, StaffOfA, ["Patient"], false },
        { "[characterization] the legacy PharmacyStaff role name is not accepted by the elevated-role gate", PharmacyA, true, true, StaffOfA, [RoleNames.PharmacyStaff], false },
        { "[characterization] plain PharmacyOwner policy trusts the staff row alone, whatever the role", PharmacyA, false, false, StaffOfA, ["Patient"], true },

        // ── malformed identities ──
        { "A token without any user id claim is denied", PharmacyA, true, true, null, [RoleNames.Pharmacy], false },
        { "A user id claim that is not a number is denied", PharmacyA, true, true, -1, [RoleNames.Pharmacy], false },
    };

    [Theory, MemberData(nameof(Matrix))]
    public async Task Grants_and_denies_exactly_as_specified(
        string scenario, int pharmacyId, bool allowPlatformAdmin, bool requireElevated,
        int? userId, string[] roles, bool expectedGrant)
    {
        await using var db = SeedStaff();
        var handler = new PharmacyOwnerHandler(db);
        var requirement = new PharmacyOwnerRequirement(allowPlatformAdmin, requireElevated);
        var pharmacy = await db.Pharmacies.AsNoTracking().SingleAsync(p => p.Id == pharmacyId);

        // -1 stands for "a NameIdentifier that is not numeric" so the theory data can stay typed int?.
        var user = userId == -1
            ? Principals.WithRawUserId("not-a-number", roles)
            : Principals.Create(userId, roles);

        var context = new AuthorizationHandlerContext([requirement], user, pharmacy);
        await handler.HandleAsync(context);

        Decision.Assert(context, expectedGrant, scenario);
    }

    [Fact]
    public async Task An_anonymous_caller_is_denied_even_if_the_policy_allows_platform_admins()
    {
        await using var db = SeedStaff();
        var handler = new PharmacyOwnerHandler(db);
        var pharmacy = await db.Pharmacies.AsNoTracking().SingleAsync(p => p.Id == PharmacyA);
        var context = new AuthorizationHandlerContext(
            [new PharmacyOwnerRequirement(true, true)], Principals.Anonymous(), pharmacy);

        await handler.HandleAsync(context);

        Decision.Assert(context, expectedGrant: false, "anonymous caller");
    }

    [Fact]
    public async Task Without_a_pharmacy_resource_even_a_SysAdmin_is_not_granted()
    {
        // This is the classic silent failure: calling AuthorizeAsync(user, "PharmacyOwnerOrAdmin") and
        // forgetting to pass the Pharmacy. A resource-based handler is never invoked without its resource,
        // so the requirement just stays unsatisfied and everyone, admins included, is refused.
        await using var db = SeedStaff();
        var handler = new PharmacyOwnerHandler(db);
        var context = new AuthorizationHandlerContext(
            [new PharmacyOwnerRequirement(true, true)], Principals.Create(1, RoleNames.SystemAdmin), resource: null);

        await handler.HandleAsync(context);

        Decision.Assert(context, expectedGrant: false, "SysAdmin without a resource");
    }
}

public class PharmacyDocumentReviewHandlerTests
{
    public static TheoryData<string, string[], bool> Matrix => new()
    {
        { "SysAdmin can review documents", [RoleNames.SystemAdmin], true },
        { "PharmacyAdmin can review documents", [RoleNames.PharmacyAdmin], true },
        { "A pharmacy owner cannot review its own documents", [RoleNames.Pharmacy], false },
        { "[characterization] the legacy PharmacyStaff role cannot review documents", [RoleNames.PharmacyStaff], false },
        { "A patient cannot review documents", ["Patient"], false },
        { "A user with no roles cannot review documents", [], false },
        { "Holding several roles still grants if any one is an admin role", ["Patient", RoleNames.PharmacyAdmin], true },
    };

    [Theory, MemberData(nameof(Matrix))]
    public async Task Grants_and_denies_exactly_as_specified(string scenario, string[] roles, bool expectedGrant)
    {
        var requirement = new PharmacyDocumentReviewRequirement();
        var context = new AuthorizationHandlerContext([requirement], Principals.Create(1, roles), resource: null);

        await new PharmacyDocumentReviewHandler().HandleAsync(context);

        Decision.Assert(context, expectedGrant, scenario);
    }

    [Fact]
    public async Task An_anonymous_caller_cannot_review_documents()
    {
        var context = new AuthorizationHandlerContext(
            [new PharmacyDocumentReviewRequirement()], Principals.Anonymous(), resource: null);

        await new PharmacyDocumentReviewHandler().HandleAsync(context);

        Decision.Assert(context, expectedGrant: false, "anonymous caller");
    }
}

public class PrescriptionVerificationHandlerTests
{
    private const int PharmacyA = 1;
    private const int PharmacyB = 2;

    private const int OrderAtA = 100;
    private const int OrderAtB = 200;

    private const int PrescriptionAtA = 1;
    private const int PrescriptionAtB = 2;
    private const int PrescriptionWithMissingOrder = 3;

    private const int PatientWhoOwnsOrderAtA = 50;
    private const int StaffOfA = 10;
    private const int InactiveStaffOfA = 11;
    private const int StaffOfB = 12;
    private const int NotStaffAnywhere = 13;

    private static PmfDbContext Seed()
    {
        var db = TestDb.Create();
        db.Pharmacies.AddRange(
            new Pharmacy { Id = PharmacyA, Name = "A", LicenceNumber = "LIC-A" },
            new Pharmacy { Id = PharmacyB, Name = "B", LicenceNumber = "LIC-B" });
        db.PharmacyStaff.AddRange(
            new PharmacyStaff { Id = 1, UserId = StaffOfA, PharmacyId = PharmacyA, Position = "Pharmacist", IsActive = true },
            new PharmacyStaff { Id = 2, UserId = InactiveStaffOfA, PharmacyId = PharmacyA, Position = "Pharmacist", IsActive = false },
            new PharmacyStaff { Id = 3, UserId = StaffOfB, PharmacyId = PharmacyB, Position = "Pharmacist", IsActive = true });
        db.Orders.AddRange(
            new Order { Id = OrderAtA, UserId = PatientWhoOwnsOrderAtA, PharmacyId = PharmacyA },
            new Order { Id = OrderAtB, UserId = 51, PharmacyId = PharmacyB });
        db.Prescriptions.AddRange(
            new Prescription { Id = PrescriptionAtA, OrderId = OrderAtA, FileUrl = "https://example.test/a" },
            new Prescription { Id = PrescriptionAtB, OrderId = OrderAtB, FileUrl = "https://example.test/b" },
            new Prescription { Id = PrescriptionWithMissingOrder, OrderId = 999, FileUrl = "https://example.test/orphan" });
        db.SaveChanges();
        return db;
    }

    // scenario, prescription id, user id, roles, expected grant
    public static TheoryData<string, int, int?, string[], bool> Matrix => new()
    {
        // ── platform admin ──
        { "SysAdmin is granted on any prescription, even without a user id claim", PrescriptionAtA, null, [RoleNames.SystemAdmin], true },
        { "SysAdmin is granted on a prescription at the other pharmacy too", PrescriptionAtB, null, [RoleNames.SystemAdmin], true },

        // ── staff of the order's pharmacy ──
        { "PharmacyAdmin with an active staff row at the order's pharmacy is granted", PrescriptionAtA, StaffOfA, [RoleNames.PharmacyAdmin], true },
        { "PharmacyStaff role with an active staff row at the order's pharmacy is granted", PrescriptionAtA, StaffOfA, [RoleNames.PharmacyStaff], true },
        { "Staff of pharmacy B is granted on pharmacy B's prescription (other direction)", PrescriptionAtB, StaffOfB, [RoleNames.PharmacyAdmin], true },

        // ── same roles, wrong pharmacy / wrong state ──
        { "Staff of pharmacy A is denied on a prescription whose order is at pharmacy B", PrescriptionAtB, StaffOfA, [RoleNames.PharmacyAdmin], false },
        { "Staff of pharmacy B is denied on a prescription whose order is at pharmacy A", PrescriptionAtA, StaffOfB, [RoleNames.PharmacyAdmin], false },
        { "An inactive staff row grants nothing", PrescriptionAtA, InactiveStaffOfA, [RoleNames.PharmacyAdmin], false },
        { "A PharmacyAdmin with no staff row is denied here (platform-wide access comes from PharmacyOwnerOrAdmin, not this handler)", PrescriptionAtA, NotStaffAnywhere, [RoleNames.PharmacyAdmin], false },
        { "A prescription whose order no longer exists grants nothing", PrescriptionWithMissingOrder, StaffOfA, [RoleNames.PharmacyAdmin], false },

        // ── roles this handler does not recognise ──
        { "[characterization] the seeded Pharmacy role is not in this handler's role list, even with a matching staff row", PrescriptionAtA, StaffOfA, [RoleNames.Pharmacy], false },
        { "The patient who owns the order is not a verifier", PrescriptionAtA, PatientWhoOwnsOrderAtA, ["Patient"], false },
        { "A user with no roles is denied", PrescriptionAtA, StaffOfA, [], false },

        // ── malformed identity ──
        { "A PharmacyAdmin token without a user id claim is denied", PrescriptionAtA, null, [RoleNames.PharmacyAdmin], false },
    };

    [Theory, MemberData(nameof(Matrix))]
    public async Task Grants_and_denies_exactly_as_specified(
        string scenario, int prescriptionId, int? userId, string[] roles, bool expectedGrant)
    {
        await using var db = Seed();
        var handler = new PrescriptionVerificationHandler(db);
        var prescription = await db.Prescriptions.AsNoTracking().SingleAsync(p => p.Id == prescriptionId);
        var context = new AuthorizationHandlerContext(
            [new PrescriptionVerificationRequirement()], Principals.Create(userId, roles), prescription);

        await handler.HandleAsync(context);

        Decision.Assert(context, expectedGrant, scenario);
    }

    [Fact]
    public async Task An_anonymous_caller_is_denied()
    {
        await using var db = Seed();
        var prescription = await db.Prescriptions.AsNoTracking().SingleAsync(p => p.Id == PrescriptionAtA);
        var context = new AuthorizationHandlerContext(
            [new PrescriptionVerificationRequirement()], Principals.Anonymous(), prescription);

        await new PrescriptionVerificationHandler(db).HandleAsync(context);

        Decision.Assert(context, expectedGrant: false, "anonymous caller");
    }
}
