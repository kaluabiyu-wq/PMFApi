using System.Text.Json.Serialization;

namespace PmfApi.Domain.Entities;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AlertEventType
{
    DocumentApproved,DocumentRejected,
    OrderPlaced,OrderConfirmed,
    OrderCancelled,PrescriptionSubmitted,
    PrescriptionApproved,PrescriptionRejected,
}


public static class AlertReferenceTables
{
    public const string Orders = "Orders";
    public const string Prescriptions = "Prescriptions";
    public const string PharmacyDocuments = "PharmacyDocuments";
}


public class Alert
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public AlertEventType EventType { get; set; }

    public required string ReferenceTable { get; set; }

    public int ReferenceId { get; set; }

    public required string Message { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}