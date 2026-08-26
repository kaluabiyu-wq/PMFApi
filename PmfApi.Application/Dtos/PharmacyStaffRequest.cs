using System.ComponentModel.DataAnnotations;

namespace PmfApi.Application.Dtos;

public record PharmacyStaffRequest
{
    public required int UserId {get;init;}

    [Required, MaxLength(100)]
    public required string Position {get;init;}

    public bool IsActive {get;init;} = true;
}
