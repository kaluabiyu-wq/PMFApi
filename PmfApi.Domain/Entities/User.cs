
namespace PmfApi.Domain.Entities;


public class User
{
    public int Id {get;set;}
    public required string FullName {get;set;}

    public required string Email {get;set;}
    public required string Password {get;set;}

    public bool IsActive {get;set;} = true;

    public DateTime CreatedAt {get;set;} = DateTime.UtcNow;

    public DateTime? LastLoginAt {get;set;}

    public DateTime? SuspendedAt {get;set;}
    public string? SuspensionReason {get;set;}

    
    public DateTime? DeletedAt {get;set;}

    
    public string? PasswordResetTokenHash {get;set;}
    public DateTime? PasswordResetExpiresAt {get;set;}

    public  required int RoleId {get;set;}
    public int LocationId {get;set;}

    public Role Role {get;set;} = null!;
   public Location Location {get;set;} = null!;

   

}