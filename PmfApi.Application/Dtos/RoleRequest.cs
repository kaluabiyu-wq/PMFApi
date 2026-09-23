
namespace PmfApi.Application.Dtos;


public record RoleRequest
{
    public required string Name {get;init;}

    public string? Description {get;init;}
}