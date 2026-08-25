using PmfApi.Dto;
using PmfApi.Entities;

public interface IPharmaciesService
{
   Task<PharmacyResponse> CreateAsync(PharmacyRequest request,CancellationToken ct);

   Task<PharmacyResponse?> GetBylicenceAsync(int id,CancellationToken ct);


    Task<PagedResponse<PharmacyResponse>> GetPharmacyAsync(PagedRequest request, CancellationToken ct);
  
}