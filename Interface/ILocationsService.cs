
using PmfApi.Dto;
using PmfApi.Entities;

namespace PmfApi.Interface;
public interface ILocationService
{

Task<LocationResponse?> GetByCoordinateAsync(int id,Coordinate coordinate,CancellationToken ct);
Task<LocationResponse> CreateAsync(LocationRequest request,CancellationToken ct);

    Task<PagedResponse<LocationResponse>> GetLocationAsync(PagedRequest request, CancellationToken ct);

}