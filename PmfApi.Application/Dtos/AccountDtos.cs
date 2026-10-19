using System.ComponentModel.DataAnnotations;

namespace PmfApi.Application.Dtos;

public record AccountResponse(
    int Id,
    string FullName,
    string Email,
    string Role,
    DateTime CreatedAt,
    int LocationId,
    string LocationLabel,
    string Subcity,
    string Woreda,
    decimal Latitude,
    decimal Longitude,
    int OrderCount,
    int OpenOrderCount,
    int FavoriteCount,
    int ReviewCount
);

public record AccountUpdateRequest
{
    [Required, MinLength(2), MaxLength(100)]
    public required string FullName { get; init; }

    [Required, EmailAddress, MaxLength(200)]
    public required string Email { get; init; }

    public int? LocationId { get; init; }
}

public record ChangePasswordRequest
{
    [Required]
    public required string CurrentPassword { get; init; }

    [Required, MinLength(8), MaxLength(100)]
    public required string NewPassword { get; init; }
}

public record DeleteAccountRequest
{
    [Required]
    public required string Password { get; init; }
}