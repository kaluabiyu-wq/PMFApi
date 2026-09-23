

namespace PmfApi.Dto;

public record InventoryHistoryRequest
{
    public required int InventoryId {get;set;}
    public  int MedicineId {get;set;}

    public  int PharmacyId {get;set;}

    public int UserId {get;set;}

    public decimal OldPrice {get;set;}


}