

using PmfApi.Application.Dtos;
using PmfApi.Domain.Entities;

namespace PmfApi.Application.Interfaces;
public interface ILocationService
{

Task<LocationResponse?> GetByCoordinateAsync(int id,Coordinate coordinate,CancellationToken ct);
Task<LocationResponse> CreateAsync(LocationRequest request,CancellationToken ct);

    Task<PagedResponse<LocationResponse>> GetLocationAsync(PagedRequest request, CancellationToken ct);

}