
namespace PmfApi.Application.Dtos;


public record UserFeedBackRequest
{
    
    public  required int UserId {get;init;}

    public required int InventoryId {get;init;}

    public required int PharmacyId {get;init;}

    public bool WasMedicineAvailable {get;init;} = true;
    public string? Comments {get;init;}
}