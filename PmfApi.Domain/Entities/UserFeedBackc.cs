

namespace PmfApi.Domain.Entities;


public class UserFeedback
{
    public int Id {get;set;}

    public int UserId {get;set;}

    public int InventoryId {get;set;}

    public int PharmacyId {get;set;}

    public bool WasMedicineAvailable {get;set;} = true;

    public DateTime SubmittedAt {get;set;} = DateTime.UtcNow;

    public string? Comments {get;set;}

    public User User {get;set;} = null!;

    public ICollection<Inventory> Inventories {get;set;} = new List<Inventory>();


}