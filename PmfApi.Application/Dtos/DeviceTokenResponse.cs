using PmfApi.Domain.Entities;

namespace PmfApi.Application.Dtos;
public record DeviceTokenResponse(
    int Id,
    DevicePlatform Platform,
    DateTime CreatedAt,
    DateTime LastSeenAt
);


public record DeviceTarget(
    int UserId, 
    DevicePlatform Platform,
    string PushToken
);