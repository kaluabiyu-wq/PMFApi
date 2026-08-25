

namespace PmfApi.Domain.Entities;

public class Inventory
{
     public int Id {get; set;}

     public int MedicineId {get;set;}

     public int PharmacyId {get;set;}

     public decimal Price {get;set;}

     public int UpdatebyUserId {get;set;}

     public string Status {get;set;} = "Fresh";

     public DateTime LastUpdatedAt {get;set;} = DateTime.UtcNow;

     public Medicine Medicine {get;set;} = null!;
     public Pharmacy Pharmacy {get;set;} = null!;

     public User User {get;set;} = null!;



}