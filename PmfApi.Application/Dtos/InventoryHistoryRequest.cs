

namespace PmfApi.Application.Dtos;

public record InventoryHistoryRequest
{
    public required int InventoryId {get;init;}
    public  int MedicineId {get;init;}

    public  int PharmacyId {get;init;}

    public int UserId {get;init;}

    public decimal OldPrice {get;init;}


}