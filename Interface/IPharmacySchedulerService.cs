using PmfApi.Dto;
using PmfApi.Entities;

public interface IPharmaciesScheduleService
{
   Task<PharmacyScheduleResponse> CreateAsync(int phamrmacyId,PharmaciesScheduleRequest request,CancellationToken ct);

   Task<PharmacyScheduleResponse?> GetByPhramacyIdAsync(int phamrmacyId,int id,CancellationToken ct);


    
}
