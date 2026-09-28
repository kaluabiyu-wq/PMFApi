using System.ComponentModel.DataAnnotations;

namespace PmfApi.Application.Dtos;

public record PharmacyUpdateRequest
{
    [MinLength(1), MaxLength(200)]
    public  string? Name {get;init;}

    [MinLength(1), MaxLength(30)]
    public  string? LicenseNumber {get;init;}

    public  int? LocationId {get;init;}

    [Range(100000000, 999999999, ErrorMessage = "PhoneNumber must be a 9-digit number.")]
    public int? PhoneNumber {get;init;}

    public string? Email {get;init;}

    [Range(0,100, ErrorMessage = "FreshnessThreshold is Measured in hours (0-100).")]
    public int? FreshnessThreshold {get;init;}
}