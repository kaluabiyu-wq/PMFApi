
namespace PmfApi.Dto;

public record InventoryRequest
{
    public required int MedicineId {get;set;}

    public required int PharmacyId {get;set;}

    public int UserId {get;set;}
    public decimal Price {get;set;}

    public string Status {get;set;} = "Fresh";

}