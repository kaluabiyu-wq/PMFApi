
namespace PmfApi.Domain.Entities;


public class Pharmacy
{
    public int Id {get;set;}

    public required string Name {get;set;}

    public required string LicenceNumber {get;set;}

    public int PhoneNumber {get;set;}

    public string? Email {get;set;}

    public bool IsVerified {get;set;}

    public decimal ReliablityScore {get;set;}

    public bool IsActive {get;set;} = true;

    public int FreshnessThreshold {get;set;}

    public DateTime LastInventoryUpdateAt {get;set;} 

   public DateTime RegisteredAt {get;set;} = DateTime.UtcNow;

   public int LocationId {get;set;}

   public Location Location {get;set;} = null!;
    public ICollection<PharmacyStaff> PharmacyStaff {get;set;} = new List<PharmacyStaff>();
    
    public ICollection<PharmacyDocument> Documents {get;set;} = new List<PharmacyDocument>();
  public ICollection<Inventory> Inventories {get;set;} = new List<Inventory>();
    public ICollection<Review> Reviews {get;set;} = new List<Review>();
    public ICollection<Favorite> Favorites {get;set;} = new List<Favorite>();
}