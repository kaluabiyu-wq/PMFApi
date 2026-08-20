
namespace PmfApi.Dto;

public record Role
{
    public required string Name {get;init;}

    public string? Description {get;init;}
}