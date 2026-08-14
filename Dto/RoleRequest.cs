
namespace PmfApi.Dto;

public record Role
{
    public required string Name {get;set;}

    public string? Description {get;set;}
}