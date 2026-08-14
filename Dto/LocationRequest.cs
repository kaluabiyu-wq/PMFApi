

namespace PmfApi.Dto;

public record LocationRequest
{
    public required string Label {get;set;}

    public required string Subcity {get;set;}

    public required decimal Latitiude {get;set;}

    public  required decimal Longitude {get;set;}

    public string? Woreda {get;set;}
}