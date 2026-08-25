
using PmfApi.Application.Dtos;
using PmfApi.Domain.Entities;


namespace PmfApi.Application.Interfaces;
public interface IPharmaciesService
{
   Task<PharmacyResponse> CreateAsync(PharmacyRequest request,CancellationToken ct);

   Task<PharmacyResponse?> GetByIdAsync(int id,CancellationToken ct);


    Task<PagedResponse<PharmacyResponse>> GetPharmacyAsync(PagedRequest request, CancellationToken ct);
  
}