namespace PmfApi.Domain.Entities;

public static class InventoryApprovalStatus
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";

    public static bool IsValid(string? value) =>
        value is Pending or Approved or Rejected;
}

public class Inventory
{
     public int Id {get; set;}

     public int MedicineId {get;set;}

     public int PharmacyId {get;set;}

     public decimal Price {get;set;}

     public int UpdatebyUserId {get;set;}

     public string Status {get;set;} = "Fresh";

     public DateTime LastUpdatedAt {get;set;} = DateTime.UtcNow;

     public string ApprovalStatus {get;set;} = InventoryApprovalStatus.Pending;

     public int? ReviewedByUserId {get;set;}

     public DateTime? ReviewedAt {get;set;}

     public string? RejectionReason {get;set;}

     public Medicine Medicine {get;set;} = null!;
     public Pharmacy Pharmacy {get;set;} = null!;

     public User User {get;set;} = null!;
}
