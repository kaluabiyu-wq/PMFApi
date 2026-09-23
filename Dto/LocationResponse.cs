
namespace PmfApi.Dto;

public record LocationResponse
(
    int Id,
    string Label,
    string Subcity,
    string Woreda,
    decimal Longitude,
    decimal Latitude

);