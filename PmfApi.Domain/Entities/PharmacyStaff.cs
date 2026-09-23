namespace PmfApi.Domain.Entities;

public class PharmacyStaff
{
    public int Id {get;set;}

    public int UserId {get;set;}

    public int PharmacyId {get;set;}

    public required string Position {get;set;}

    public bool IsActive {get;set;} = true;

    public DateTime AssignedAt {get;set;} = DateTime.UtcNow;

    public User User {get;set;} = null!;
    public Pharmacy Pharmacy {get;set;} = null!;
}
