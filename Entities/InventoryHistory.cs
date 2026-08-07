
namespace PmfApi.Entities;

public class InventoryHistory
{
    public int Id {get;set;}

    public int InventoryId {get;set;}
    public int MedicineId {get;set;}

    public int PharmacyId {get;set;}

    public decimal OldPrice {get;set;}

    public int UserId {get;set;}

    public DateTime ChangedAt {get;set;} = DateTime.UtcNow;

     public Medicine Medicine {get;set;} = null!;
     public Pharmacy Pharmacy {get;set;} = null!;

     public Inventory Inventory {get;set;} = null!;

   public User User {get;set;} = null!;
  

}