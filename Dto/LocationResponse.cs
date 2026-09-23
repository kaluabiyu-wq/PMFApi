
using PmfApi.Entities;

namespace PmfApi.Dto;

public record LocationResponse
(
    int Id,
    string Label,
    string Subcity,
    string Woreda,
    Coordinate Coordinate

);