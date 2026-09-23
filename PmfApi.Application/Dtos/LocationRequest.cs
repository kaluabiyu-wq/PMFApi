

using PmfApi.Domain.Entities;

namespace PmfApi.Application.Dtos;


public record LocationRequest
{
    public required string Label {get;init;}

    public required string Subcity {get;init;}

     public Coordinate Coordinate {get;init;} = new();
    public required string Woreda {get;init;}
}