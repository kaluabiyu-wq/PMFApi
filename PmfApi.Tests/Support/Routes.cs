namespace PmfApi.Tests.Support;


public static class Routes
{
    public const string Orders = "/api/orders";
    public const string Alerts = "/api/alerts";

    public static string ConfirmOrder(int orderId) => $"/api/orders/{orderId}/confirm";
    public static string OrderPrescriptions(int orderId) => $"/api/orders/{orderId}/prescriptions";

    public static string Prescription(int id) => $"/api/prescriptions/{id}";
    public static string ReviewPrescription(int id) => $"/api/prescriptions/{id}/review";

    public static string PharmacyPrescriptions(int pharmacyId, string query = "") =>
        $"/api/pharmacies/{pharmacyId}/prescriptions{query}";

    public static string AdminPharmacy(int pharmacyId) => $"/api/admin/pharmacies/{pharmacyId}";
}
