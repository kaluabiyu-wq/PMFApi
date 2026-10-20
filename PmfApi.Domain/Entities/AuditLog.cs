namespace PmfApi.Domain.Entities;

public class AuditLog
{
    public int Id { get; set; }

    public int ActorUserId { get; set; }

    public required string ActorName { get; set; }

    public required string Action { get; set; }

    public required string TargetType { get; set; }

    public int TargetId { get; set; }

    public string? Detail { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}