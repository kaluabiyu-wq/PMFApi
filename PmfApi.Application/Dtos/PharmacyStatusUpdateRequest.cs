using System.ComponentModel.DataAnnotations;

namespace PmfApi.Application.Dtos;

public record PharmacyStatusUpdateRequest
{
    public required bool IsVerified {get;init;}

    public required bool IsActive {get;init;}

    [MaxLength(500)]
    public string? Reason {get;init;}
}