namespace PmfApi.Application.Abstractions;

public static class PmfHubEvents
{
    public const string InventoryStatusChanged = "ReceiveInventoryStatusChanged";
    public const string OrderCreated           = "ReceiveOrderCreated";
    public const string OrderStatusChanged     = "ReceiveOrderStatusChanged";
    public const string PrescriptionVerified   = "ReceivePrescriptionVerified";
}

public static class PmfGroups
{
    public static string Pharmacy(int id) => $"pharmacy:{id}";
    public static string Medicine(int id) => $"medicine:{id}";
    public static string Role(string role) => $"role:{role}";
    public static string User(string userId) => $"user:{userId}";
}