using PmfApi.Dto;
using PmfApi.Entities;

public interface IPharmaciesService
{
   Task<PharmacyResponse> CreateAsync(PharmacyRequest request,CancellationToken ct);

   Task<PharmacyResponse?> GetBylicenceAsync(string licenceNumber,CancellationToken ct);

}