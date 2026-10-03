using System.ComponentModel.DataAnnotations;
using PmfApi.Domain.Entities;

namespace PmfApi.Application.Dtos;

public record DeviceTokenRequest
{
    [Required]
    public required DevicePlatform Platform { get; init; }

    [Required, StringLength(1024, MinimumLength = 16)]
    public required string PushToken { get; init; }
}
public record DeviceTokenUnregisterRequest
{
    [Required, StringLength(1024, MinimumLength = 16)]
    public required string PushToken { get; init; }
}