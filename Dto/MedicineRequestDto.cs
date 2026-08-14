

namespace PmfApi.Dto;

public record MedicineRequest
{
    public required string GenericName {get;set;}

    public required string BrandName {get;set;}

    public string? Category {get;set;}
    public string? DosageForm {get;set;}

    public bool RequeiresPresciption {get;set;} = true;

    public bool IsActice {get;set;} = true;

}