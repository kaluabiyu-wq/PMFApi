using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PmfApi.Application.Dtos;
using PmfApi.Domain.Entities;
using PmfApi.Tests.Support;

namespace PmfApi.Tests;

// ─────────────────────────────────────────────────────────────────────────────────────────────────
// Step 16: the prescription-verification workflow, end to end.
//
// These tests go through the real pipeline (JWT validation → policies → controllers → services → EF →
// PostgreSQL), because the rules that matter here live in the seams between those layers:
//   * the service uses ExecuteUpdate and transactions, which the in-memory provider cannot run;
//   * "one Pending prescription per order" is partly enforced by a PostgreSQL partial unique index;
//   * who may see or decide a prescription is decided by a handler PLUS a controller fallback.
// They need PostgreSQL; see PostgresFixture. Each test creates its own orders, so tests never depend on
// each other's data or on run order.
// ─────────────────────────────────────────────────────────────────────────────────────────────────
[Collection(PostgresCollection.Name)]
public class PrescriptionWorkflowTests(PostgresFixture fx)
{
    private TestWorld W => fx.World;
    private HttpClient As(TestUser? user) => fx.Factory.ClientFor(user);

    // ── helpers ──────────────────────────────────────────────────────────────────────────────────

    private static async Task AssertStatusAsync(HttpStatusCode expected, HttpResponseMessage response)
    {
        if (response.StatusCode == expected) return;

        Assert.Fail(
            $"Expected {(int)expected} {expected} but got {(int)response.StatusCode} {response.StatusCode}. " +
            $"Body: {await response.Content.ReadAsStringAsync()}");
    }

    private async Task<OrderResponse> PlaceOrderAsync(TestUser patient, int inventoryId)
    {
        var response = await As(patient).PostAsJsonAsync(
            Routes.Orders,
            new OrderRequest { Items = [new OrderItemRequest { InventoryId = inventoryId, Quantity = 1 }] },
            Json.Options);

        await AssertStatusAsync(HttpStatusCode.Created, response);
        return (await response.Content.ReadFromJsonAsync<OrderResponse>(Json.Options))!;
    }

    private Task<HttpResponseMessage> SubmitAsync(TestUser who, int orderId, string fileUrl = "https://example.test/rx.jpg") =>
        As(who).PostAsJsonAsync(Routes.OrderPrescriptions(orderId), new PrescriptionRequest { FileUrl = fileUrl }, Json.Options);

    private async Task<PrescriptionResponse> SubmitOkAsync(TestUser who, int orderId)
    {
        var response = await SubmitAsync(who, orderId);
        await AssertStatusAsync(HttpStatusCode.Created, response);
        return (await response.Content.ReadFromJsonAsync<PrescriptionResponse>(Json.Options))!;
    }

    private Task<HttpResponseMessage> ReviewAsync(TestUser who, int prescriptionId, VerificationStatus decision, string? note = null) =>
        As(who).PatchAsync(
            Routes.ReviewPrescription(prescriptionId),
            JsonContent.Create(new PrescriptionReviewRequest { VerificationStatus = decision, Note = note }, options: Json.Options));

    private Task<HttpResponseMessage> ConfirmAsync(TestUser who, int orderId) =>
        As(who).PostAsync(Routes.ConfirmOrder(orderId), content: null);

    private async Task<PrescriptionResponse> ReadPrescriptionAsync(TestUser who, int prescriptionId)
    {
        var response = await As(who).GetAsync(Routes.Prescription(prescriptionId));
        await AssertStatusAsync(HttpStatusCode.OK, response);
        return (await response.Content.ReadFromJsonAsync<PrescriptionResponse>(Json.Options))!;
    }

    private async Task<OrderResponse> ReadOrderAsync(TestUser who, int orderId)
    {
        var response = await As(who).GetAsync($"{Routes.Orders}/{orderId}");
        await AssertStatusAsync(HttpStatusCode.OK, response);
        return (await response.Content.ReadFromJsonAsync<OrderResponse>(Json.Options))!;
    }

    private async Task<List<AlertResponse>> AlertsAsync(TestUser who)
    {
        // Newest first, and tests in a class run one after another, so this test's alerts are on page 1.
        var response = await As(who).GetAsync($"{Routes.Alerts}?pageSize=50");
        await AssertStatusAsync(HttpStatusCode.OK, response);
        var page = await response.Content.ReadFromJsonAsync<PagedResponse<AlertResponse>>(Json.Options);
        return page!.Items.ToList();
    }

    // ── the main flow ────────────────────────────────────────────────────────────────────────────

    [RequiresPostgresFact]
    public async Task An_Rx_order_is_confirmable_only_after_its_prescription_is_approved()
    {
        var order = await PlaceOrderAsync(W.PatientA, W.RxInventoryA);
        Assert.True(order.RequiresPrescription);
        Assert.Null(order.PrescriptionStatus);

        // No prescription yet.
        await AssertStatusAsync(HttpStatusCode.Conflict, await ConfirmAsync(W.OwnerA, order.Id));

        var prescription = await SubmitOkAsync(W.PatientA, order.Id);
        Assert.Equal(VerificationStatus.Pending, prescription.VerificationStatus);
        Assert.Equal(order.Id, prescription.OrderId);
        Assert.Null(prescription.VerifiedByUserId);

        // Submitted but not yet reviewed is still not enough.
        await AssertStatusAsync(HttpStatusCode.Conflict, await ConfirmAsync(W.OwnerA, order.Id));

        var review = await ReviewAsync(W.OwnerA, prescription.Id, VerificationStatus.Approved);
        await AssertStatusAsync(HttpStatusCode.OK, review);
        var approved = (await review.Content.ReadFromJsonAsync<PrescriptionResponse>(Json.Options))!;
        Assert.Equal(VerificationStatus.Approved, approved.VerificationStatus);
        Assert.Equal(W.OwnerA.Id, approved.VerifiedByUserId);   // the verifier comes from the token, not the body
        Assert.NotNull(approved.VerifiedAt);

        var confirm = await ConfirmAsync(W.OwnerA, order.Id);
        await AssertStatusAsync(HttpStatusCode.OK, confirm);
        Assert.Equal(OrderStatus.Confirmed, (await ReadOrderAsync(W.PatientA, order.Id)).Status);
    }

    [RequiresPostgresFact]
    public async Task A_rejection_needs_a_note_and_a_rejected_prescription_can_be_replaced()
    {
        var order = await PlaceOrderAsync(W.PatientA, W.RxInventoryA);
        var first = await SubmitOkAsync(W.PatientA, order.Id);

        // Rejecting without a reason is refused and leaves the prescription pending.
        await AssertStatusAsync(HttpStatusCode.BadRequest, await ReviewAsync(W.OwnerA, first.Id, VerificationStatus.Rejected));
        await AssertStatusAsync(HttpStatusCode.BadRequest, await ReviewAsync(W.OwnerA, first.Id, VerificationStatus.Rejected, note: "   "));
        Assert.Equal(VerificationStatus.Pending, (await ReadPrescriptionAsync(W.OwnerA, first.Id)).VerificationStatus);

        await AssertStatusAsync(HttpStatusCode.OK, await ReviewAsync(W.OwnerA, first.Id, VerificationStatus.Rejected, "Photo is unreadable."));
        var rejected = await ReadPrescriptionAsync(W.PatientA, first.Id);
        Assert.Equal(VerificationStatus.Rejected, rejected.VerificationStatus);
        Assert.Equal("Photo is unreadable.", rejected.ReviewNote);

        // A rejected prescription does not unlock confirmation.
        await AssertStatusAsync(HttpStatusCode.Conflict, await ConfirmAsync(W.OwnerA, order.Id));

        // The patient replaces it; the approved replacement unlocks the order.
        var second = await SubmitOkAsync(W.PatientA, order.Id);
        Assert.NotEqual(first.Id, second.Id);
        await AssertStatusAsync(HttpStatusCode.OK, await ReviewAsync(W.OwnerA, second.Id, VerificationStatus.Approved));
        await AssertStatusAsync(HttpStatusCode.OK, await ConfirmAsync(W.OwnerA, order.Id));
    }

    [RequiresPostgresFact]
    public async Task A_prescription_can_be_decided_only_once()
    {
        var order = await PlaceOrderAsync(W.PatientA, W.RxInventoryA);
        var prescription = await SubmitOkAsync(W.PatientA, order.Id);
        await AssertStatusAsync(HttpStatusCode.OK, await ReviewAsync(W.OwnerA, prescription.Id, VerificationStatus.Approved));

        await AssertStatusAsync(HttpStatusCode.Conflict, await ReviewAsync(W.OwnerA, prescription.Id, VerificationStatus.Approved));
        await AssertStatusAsync(HttpStatusCode.Conflict, await ReviewAsync(W.SysAdmin, prescription.Id, VerificationStatus.Rejected, "changed my mind"));

        Assert.Equal(VerificationStatus.Approved, (await ReadPrescriptionAsync(W.PatientA, prescription.Id)).VerificationStatus);
    }

    [RequiresPostgresFact]
    public async Task Only_Approved_or_Rejected_are_valid_review_decisions()
    {
        var order = await PlaceOrderAsync(W.PatientA, W.RxInventoryA);
        var prescription = await SubmitOkAsync(W.PatientA, order.Id);

        await AssertStatusAsync(HttpStatusCode.BadRequest, await ReviewAsync(W.OwnerA, prescription.Id, VerificationStatus.Pending));
    }

    [RequiresPostgresFact]
    public async Task Only_one_prescription_can_await_review_at_a_time()
    {
        var order = await PlaceOrderAsync(W.PatientA, W.RxInventoryA);
        await SubmitOkAsync(W.PatientA, order.Id);

        await AssertStatusAsync(HttpStatusCode.Conflict, await SubmitAsync(W.PatientA, order.Id));
    }

    [RequiresPostgresFact]
    public async Task Nothing_more_can_be_submitted_once_a_prescription_is_approved()
    {
        var order = await PlaceOrderAsync(W.PatientA, W.RxInventoryA);
        var prescription = await SubmitOkAsync(W.PatientA, order.Id);
        await AssertStatusAsync(HttpStatusCode.OK, await ReviewAsync(W.OwnerA, prescription.Id, VerificationStatus.Approved));

        await AssertStatusAsync(HttpStatusCode.Conflict, await SubmitAsync(W.PatientA, order.Id));
    }

    [RequiresPostgresFact]
    public async Task An_order_with_only_over_the_counter_medicine_needs_no_prescription()
    {
        var order = await PlaceOrderAsync(W.PatientA, W.OtcInventoryA);
        Assert.False(order.RequiresPrescription);

        await AssertStatusAsync(HttpStatusCode.BadRequest, await SubmitAsync(W.PatientA, order.Id));

        // ...and it confirms without one.
        await AssertStatusAsync(HttpStatusCode.OK, await ConfirmAsync(W.OwnerA, order.Id));
    }

    [RequiresPostgresFact]
    public async Task A_prescription_cannot_be_submitted_for_a_cancelled_order()
    {
        var order = await PlaceOrderAsync(W.PatientA, W.RxInventoryA);
        await AssertStatusAsync(HttpStatusCode.OK, await As(W.PatientA).PostAsync($"{Routes.Orders}/{order.Id}/cancel", content: null));

        await AssertStatusAsync(HttpStatusCode.Conflict, await SubmitAsync(W.PatientA, order.Id));
    }

    [RequiresPostgresFact]
    public async Task A_link_must_be_an_absolute_http_or_https_url()
    {
        var order = await PlaceOrderAsync(W.PatientA, W.RxInventoryA);

        await AssertStatusAsync(HttpStatusCode.BadRequest, await SubmitAsync(W.PatientA, order.Id, "javascript:alert(1)"));
        await AssertStatusAsync(HttpStatusCode.BadRequest, await SubmitAsync(W.PatientA, order.Id, "/relative/path.jpg"));
    }

    // ── who may see or decide ────────────────────────────────────────────────────────────────────

    [RequiresPostgresFact]
    public async Task Another_patient_cannot_submit_for_someone_elses_order()
    {
        var order = await PlaceOrderAsync(W.PatientA, W.RxInventoryA);

        // 404, not 403: order ids must not be probeable.
        await AssertStatusAsync(HttpStatusCode.NotFound, await SubmitAsync(W.PatientB, order.Id));
    }

    [RequiresPostgresFact]
    public async Task Staff_of_a_different_pharmacy_cannot_see_or_decide_the_prescription()
    {
        var order = await PlaceOrderAsync(W.PatientA, W.RxInventoryA);   // placed at pharmacy A
        var prescription = await SubmitOkAsync(W.PatientA, order.Id);

        await AssertStatusAsync(HttpStatusCode.NotFound, await As(W.OwnerB).GetAsync(Routes.Prescription(prescription.Id)));
        await AssertStatusAsync(HttpStatusCode.NotFound, await As(W.OwnerB).GetAsync(Routes.OrderPrescriptions(order.Id)));
        await AssertStatusAsync(HttpStatusCode.NotFound, await ReviewAsync(W.OwnerB, prescription.Id, VerificationStatus.Approved));
        await AssertStatusAsync(HttpStatusCode.Forbidden, await As(W.OwnerB).GetAsync(Routes.PharmacyPrescriptions(W.PharmacyAId)));

        // Nothing leaked and nothing changed.
        Assert.Equal(VerificationStatus.Pending, (await ReadPrescriptionAsync(W.PatientA, prescription.Id)).VerificationStatus);
    }

    [RequiresPostgresFact]
    public async Task Another_patient_cannot_see_the_prescription()
    {
        var order = await PlaceOrderAsync(W.PatientA, W.RxInventoryA);
        var prescription = await SubmitOkAsync(W.PatientA, order.Id);

        await AssertStatusAsync(HttpStatusCode.NotFound, await As(W.PatientB).GetAsync(Routes.Prescription(prescription.Id)));
        await AssertStatusAsync(HttpStatusCode.NotFound, await As(W.PatientB).GetAsync(Routes.OrderPrescriptions(order.Id)));
    }

    [RequiresPostgresFact]
    public async Task The_patient_cannot_approve_their_own_prescription()
    {
        var order = await PlaceOrderAsync(W.PatientA, W.RxInventoryA);
        var prescription = await SubmitOkAsync(W.PatientA, order.Id);

        await AssertStatusAsync(HttpStatusCode.Forbidden, await ReviewAsync(W.PatientA, prescription.Id, VerificationStatus.Approved));

        Assert.Equal(VerificationStatus.Pending, (await ReadPrescriptionAsync(W.PatientA, prescription.Id)).VerificationStatus);
    }

    [RequiresPostgresFact]
    public async Task A_SysAdmin_and_a_PharmacyAdmin_can_each_decide_any_pharmacys_prescription()
    {
        var order1 = await PlaceOrderAsync(W.PatientA, W.RxInventoryA);
        var rx1 = await SubmitOkAsync(W.PatientA, order1.Id);
        var order2 = await PlaceOrderAsync(W.PatientA, W.RxInventoryB);   // a different pharmacy again
        var rx2 = await SubmitOkAsync(W.PatientA, order2.Id);

        var bySysAdmin = await ReviewAsync(W.SysAdmin, rx1.Id, VerificationStatus.Approved);
        await AssertStatusAsync(HttpStatusCode.OK, bySysAdmin);
        Assert.Equal(W.SysAdmin.Id, (await ReadPrescriptionAsync(W.PatientA, rx1.Id)).VerifiedByUserId);

        var byPharmacyAdmin = await ReviewAsync(W.PharmacyAdmin, rx2.Id, VerificationStatus.Rejected, "Expired prescription.");
        await AssertStatusAsync(HttpStatusCode.OK, byPharmacyAdmin);
        Assert.Equal(W.PharmacyAdmin.Id, (await ReadPrescriptionAsync(W.PatientA, rx2.Id)).VerifiedByUserId);
    }

    [RequiresPostgresFact]
    public async Task A_pharmacy_owner_with_the_Pharmacy_role_can_decide_via_the_owner_policy_fallback()
    {
        // PrescriptionVerificationHandler does not list the seeded "Pharmacy" role; the controller's
        // fallback to PharmacyOwnerOrAdmin is what lets the owner work. If someone removes that fallback
        // this test fails, which is the point: owners would silently lose access.
        var order = await PlaceOrderAsync(W.PatientA, W.RxInventoryA);
        var prescription = await SubmitOkAsync(W.PatientA, order.Id);

        await AssertStatusAsync(HttpStatusCode.OK, await As(W.OwnerA).GetAsync(Routes.Prescription(prescription.Id)));
        await AssertStatusAsync(HttpStatusCode.OK, await ReviewAsync(W.OwnerA, prescription.Id, VerificationStatus.Approved));
    }

    // ── the pharmacy's work queue ────────────────────────────────────────────────────────────────

    [RequiresPostgresFact]
    public async Task The_pending_queue_is_scoped_to_the_pharmacy_and_lists_oldest_first()
    {
        var orderAtA = await PlaceOrderAsync(W.PatientA, W.RxInventoryA);
        var rxAtA = await SubmitOkAsync(W.PatientA, orderAtA.Id);
        var orderAtB = await PlaceOrderAsync(W.PatientA, W.RxInventoryB);
        var rxAtB = await SubmitOkAsync(W.PatientA, orderAtB.Id);

        var responseA = await As(W.OwnerA).GetAsync(Routes.PharmacyPrescriptions(W.PharmacyAId, "?status=Pending&pageSize=50"));
        await AssertStatusAsync(HttpStatusCode.OK, responseA);
        var queueA = (await responseA.Content.ReadFromJsonAsync<PagedResponse<PrescriptionResponse>>(Json.Options))!.Items;

        Assert.Contains(queueA, p => p.Id == rxAtA.Id);
        Assert.DoesNotContain(queueA, p => p.Id == rxAtB.Id);              // another pharmacy's work
        Assert.All(queueA, p => Assert.Equal(VerificationStatus.Pending, p.VerificationStatus));
        Assert.Equal(queueA.OrderBy(p => p.SubmittedAt).ThenBy(p => p.Id).Select(p => p.Id), queueA.Select(p => p.Id));

        var responseB = await As(W.OwnerB).GetAsync(Routes.PharmacyPrescriptions(W.PharmacyBId, "?status=Pending&pageSize=50"));
        await AssertStatusAsync(HttpStatusCode.OK, responseB);
        var queueB = (await responseB.Content.ReadFromJsonAsync<PagedResponse<PrescriptionResponse>>(Json.Options))!.Items;
        Assert.Contains(queueB, p => p.Id == rxAtB.Id);
        Assert.DoesNotContain(queueB, p => p.Id == rxAtA.Id);
    }

    // ── alerts raised by the workflow ────────────────────────────────────────────────────────────

    [RequiresPostgresFact]
    public async Task The_workflow_alerts_the_right_people_about_the_right_rows()
    {
        var order = await PlaceOrderAsync(W.PatientA, W.RxInventoryA);
        var prescription = await SubmitOkAsync(W.PatientA, order.Id);

        // Pharmacy staff are told a prescription is waiting.
        Assert.Contains(await AlertsAsync(W.OwnerA), a =>
            a.EventType == AlertEventType.PrescriptionSubmitted
            && a.ReferenceTable == AlertReferenceTables.Prescriptions
            && a.ReferenceId == prescription.Id);

        await AssertStatusAsync(HttpStatusCode.OK, await ReviewAsync(W.OwnerA, prescription.Id, VerificationStatus.Approved));
        await AssertStatusAsync(HttpStatusCode.OK, await ConfirmAsync(W.OwnerA, order.Id));

        // The patient is told about the decision and then about the confirmation.
        var patientAlerts = await AlertsAsync(W.PatientA);
        Assert.Contains(patientAlerts, a =>
            a.EventType == AlertEventType.PrescriptionApproved
            && a.ReferenceTable == AlertReferenceTables.Prescriptions
            && a.ReferenceId == prescription.Id);
        Assert.Contains(patientAlerts, a =>
            a.EventType == AlertEventType.OrderConfirmed
            && a.ReferenceTable == AlertReferenceTables.Orders
            && a.ReferenceId == order.Id);

        // The other pharmacy's staff were not told about any of this.
        Assert.DoesNotContain(await AlertsAsync(W.OwnerB), a => a.ReferenceId == prescription.Id
            && a.ReferenceTable == AlertReferenceTables.Prescriptions);
    }

    [RequiresPostgresFact]
    public async Task A_rejection_alerts_the_patient()
    {
        var order = await PlaceOrderAsync(W.PatientA, W.RxInventoryA);
        var prescription = await SubmitOkAsync(W.PatientA, order.Id);

        await AssertStatusAsync(HttpStatusCode.OK, await ReviewAsync(W.OwnerA, prescription.Id, VerificationStatus.Rejected, "Not legible."));

        Assert.Contains(await AlertsAsync(W.PatientA), a =>
            a.EventType == AlertEventType.PrescriptionRejected && a.ReferenceId == prescription.Id);
    }

    // ── concurrency and the database backstop ────────────────────────────────────────────────────

    [RequiresPostgresFact]
    public async Task Two_reviewers_deciding_at_the_same_moment_produce_exactly_one_winner()
    {
        var order = await PlaceOrderAsync(W.PatientA, W.RxInventoryA);
        var prescription = await SubmitOkAsync(W.PatientA, order.Id);

        var approve = ReviewAsync(W.OwnerA, prescription.Id, VerificationStatus.Approved);
        var reject = ReviewAsync(W.SysAdmin, prescription.Id, VerificationStatus.Rejected, "Disagree.");
        var responses = await Task.WhenAll(approve, reject);

        var statuses = responses.Select(r => r.StatusCode).OrderBy(s => (int)s).ToArray();
        Assert.Equal(new[] { HttpStatusCode.OK, HttpStatusCode.Conflict }, statuses);

        // The stored decision belongs to whoever got the 200, and only to them.
        var winnerIsApprover = responses[0].StatusCode == HttpStatusCode.OK;   // responses[0] is the approve call
        var stored = await ReadPrescriptionAsync(W.PatientA, prescription.Id);
        Assert.Equal(winnerIsApprover ? VerificationStatus.Approved : VerificationStatus.Rejected, stored.VerificationStatus);
        Assert.Equal(winnerIsApprover ? W.OwnerA.Id : W.SysAdmin.Id, stored.VerifiedByUserId);
    }

    [RequiresPostgresFact]
    public async Task The_database_itself_refuses_two_pending_prescriptions_for_one_order()
    {
        // The service checks first, but two requests can race past that check. The partial unique index
        // on (OrderId) WHERE VerificationStatus = 'Pending' is the backstop, so go straight to the table.
        await using var db = fx.NewDbContext();
        var order = new Order { UserId = W.PatientA.Id, PharmacyId = W.PharmacyAId };
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        db.Prescriptions.AddRange(
            new Prescription { OrderId = order.Id, FileUrl = "https://example.test/one" },
            new Prescription { OrderId = order.Id, FileUrl = "https://example.test/two" });

        var failure = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        var postgres = Assert.IsType<PostgresException>(failure.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, postgres.SqlState);
    }
}
