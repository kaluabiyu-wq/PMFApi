using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace PmfApi.Application.Dtos;

public class RegisterPharmacyRequest
{
    // Owner account
    [Required, MaxLength(200)]
     public string FullName { get; set; } = "";
    [Required, EmailAddress, MaxLength(200)] 
    public string Email { get; set; } = "";
    [Required, MinLength(8)]
     public string Password { get; set; } = "";
    [Range(1, int.MaxValue)] 
    public int LocationId { get; set; }

    // Pharmacy
    [Required, MaxLength(200)] 
    public string PharmacyName { get; set; } = "";

    [Required, MaxLength(100)] 
    public string LicenseNumber { get; set; } = "";

    [Range(100000000, 999999999, ErrorMessage = "PhoneNumber must be a 9-digit number.")]
    public int PhoneNumber { get; set; }

   //verification documents
    [Required]
     public IFormFile License { get; set; } = null!;
    [Required]
     public IFormFile BusinessRegistration { get; set; } = null!;
    [Required]
     public IFormFile PharmacistCredential { get; set; } = null!;
}