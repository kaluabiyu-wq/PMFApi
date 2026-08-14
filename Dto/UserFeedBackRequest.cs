
namespace PmfApi.Dto;

public record UserFeedBackRequest
{
    
    public  required int UserId {get;set;}

    public required int InventoryId {get;set;}

    public required int PharmacyId {get;set;}

    public bool WasMedicineAvailable {get;set;} = true;
    public string? Comments {get;set;}
}