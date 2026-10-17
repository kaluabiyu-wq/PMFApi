using System.ComponentModel.DataAnnotations;

namespace PmfApi.Application.Dtos;
public record MedicineRequest
{
    [Required, MaxLength(200)]
    public required string GenericName {get;init;}

    [MaxLength(200)]
    public string BrandName {get;init;} = string.Empty;

    [MaxLength(200)]
    public string? Category {get;init;}

    [MaxLength(100)]
    public string? DosageForm {get;init;}

    [MaxLength(100)]
    public string? Strength {get;init;}

    public bool RequiresPrescription {get;init;} = true;
}
