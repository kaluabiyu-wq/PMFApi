

using PmfApi.Entities;

namespace PmfApi.Dto;

public record LocationRequest
{
    public required string Label {get;init;}

    public required string Subcity {get;init;}

     public Coordinate Coordinate {get;init;} = new();
    public string? Woreda {get;init;}
}