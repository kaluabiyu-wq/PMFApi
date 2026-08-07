
namespace PmfApi.Entities;

public class Medicine
{
    public int Id {get;set;}

   public required string GenericName {get;set;}

   public required string BrandName {get;set;}

   public string? Category {get;set;}

   public string? DosageForm {get;set;}

   public string? Strength {get;set;}

   public bool RequeiresPrescription {get;set;} = true;

   public bool IsActive {get;set;} = true;

   public ICollection<Inventory> Inventories {get;set;} = new List<Inventory>();


}