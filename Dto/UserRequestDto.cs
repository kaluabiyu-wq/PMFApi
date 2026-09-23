
namespace PmfApi.Dto;

public record UserRequest
{
    public required string FullName {get;set;}

    public required string Email {get;set;}

    public required string Password {get;set;}

    public required int LocationId {get;set;}

    public int RoleId {get;set;}


    public bool IsActice {get;set;} = true;

}