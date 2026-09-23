
using PmfApi.Application.Dtos;
using PmfApi.Domain.Entities;


namespace PmfApi.Application.Interfaces;
public interface IPharmaciesScheduleService
{
   Task<PharmacyScheduleResponse> CreateAsync(int phamrmacyId,PharmaciesScheduleRequest request,CancellationToken ct);

   Task<PharmacyScheduleResponse?> GetByPhramacyIdAsync(int phamrmacyId,int id,CancellationToken ct);


   //  Task<PagedResponse<PharmacyScheduleResponse>> GetPharmacyscheduleAsync(PagedRequest request, CancellationToken ct);
  
    
}
