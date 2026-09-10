namespace PmfApi.Domain.Entities;


public class Favorite
{
    public int Id {get;set;}

    public int UserId {get;set;}

    public int PharmacyId {get;set;}

    public DateTime CreatedAt {get;set;} = DateTime.UtcNow;

    public User User {get;set;} = null!;
    public Pharmacy Pharmacy {get;set;} = null!;
}
