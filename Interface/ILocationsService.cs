
using PmfApi.Dto;
using PmfApi.Entities;

namespace PmfApi.Interface;
public interface ILocationService
{

Task<LocationResponse?> GetByLongitudAsync(decimal longitude, decimal latitude,CancellationToken ct);
Task<LocationResponse> CreateAsync(LocationRequest request,CancellationToken ct);

}