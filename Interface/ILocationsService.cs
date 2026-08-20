
using PmfApi.Dto;
using PmfApi.Entities;

namespace PmfApi.Interface;
public interface ILocationService
{

Task<LocationResponse?> GetByCoordinateAsync(Coordinate coordinate,CancellationToken ct);
Task<LocationResponse> CreateAsync(LocationRequest request,CancellationToken ct);

}