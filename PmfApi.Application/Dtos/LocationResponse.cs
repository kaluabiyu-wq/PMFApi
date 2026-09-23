
using PmfApi.Domain.Entities;

namespace PmfApi.Application.Dtos;


public record LocationResponse
(
    int Id,
    string Label,
    string Subcity,
    string Woreda,
    Coordinate Coordinate

);