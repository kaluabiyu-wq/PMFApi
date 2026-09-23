

using System.ComponentModel.DataAnnotations;

namespace PmfApi.Dto;

public record MedicineRequest
{
    [Required, MaxLength(200)]
    public required string GenericName {get;init;}

    [Required, MaxLength(200)]
    public required string BrandName {get;init;}


    public bool RequeiresPresciption {get;init;} = true;

    public bool IsActice {get;init;} = true;

}