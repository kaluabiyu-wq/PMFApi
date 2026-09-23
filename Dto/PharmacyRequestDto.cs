
using System.ComponentModel.DataAnnotations;

namespace PmfApi.Dto;

public record PharmacyRequest
{
    [Required, MaxLength(200)]
    public required string Name {get;init;}


    [Required, MaxLength(30)]
    public required string LicenseNumber {get;init;}

    
    public required int LocationId {get;init;}

    [Range(100000000, 999999999, ErrorMessage = "PhoneNumber must be a 9-digit number.")]
    public int PhoneNumber {get;init;}


    public string? Email {get;init;}

    public bool IsVerified {get;init;} = true;

     [Range(0,100, ErrorMessage = "FreshnessThreshold is Measured in hours (0-100).")]
    public int FreshnessThreshold {get;init;}

}