
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

  public ICollection<Inventory> Inventories {get;set;} = new List<Inventory>();
}