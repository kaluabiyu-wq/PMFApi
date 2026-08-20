
namespace PmfApi.Dto;

public record UserRequest
{
    public required string FullName {get;init;}

    public required string Email {get;init;}

    public required string Password {get;init;}

    public required int LocationId {get;init;}

    public required int RoleId {get;init;}


    public bool IsActice {get;init;} = true;

}