using System.Text.Json.Serialization;

namespace PmfApi.Domain.Entities;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DevicePlatform
{
    Android,Ios,Web,
}

public class DeviceToken
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public DevicePlatform Platform { get; set; }

    public required string PushToken { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}