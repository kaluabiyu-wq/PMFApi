using System.Text.Json.Serialization;

namespace PmfApi.Domain.Entities;


[JsonConverter(typeof(JsonStringEnumConverter))]
public enum VerificationStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
}


public class Prescription
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public required string FileUrl { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Pending;

    public int? VerifiedByUserId { get; set; }

    public DateTime? VerifiedAt { get; set; }

    public string? ReviewNote { get; set; }

    public Order Order { get; set; } = null!;
    public User? VerifiedByUser { get; set; }
}