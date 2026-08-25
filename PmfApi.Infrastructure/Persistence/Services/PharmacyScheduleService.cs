using Microsoft.EntityFrameworkCore;
using PmfApi.Infrastructure.Persistence;
using PmfApi.Application.Dtos;
using PmfApi.Domain.Entities;
using PmfApi.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace PmfApi.Infrastructure.Persistence.Services;

public class PharmaciesScheduleService(PmfDbContext context, ILogger<PharmaciesScheduleService> logger)
: IPharmaciesScheduleService
{
   public async Task<PharmacyScheduleResponse> CreateAsync(int phamrmacyId,PharmaciesScheduleRequest request,CancellationToken ct)
    {
         var schedule = new PharmaciesSchedule
        {
           PharmacyId = phamrmacyId,
           DayOfWeek = request.DayOfWeek,
           OpenTime = DateTime.UtcNow,
           ClosedTime = DateTime.UtcNow,
            
        };
        context.PharmaciesSchedules.Add(schedule);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("Created Pharmacies Schedule {PhramacyScheduleId} for Pharmacy {PharmacyId} with {DayofWeek} {OpenTime} {ClosedTime}",
            schedule.Id,schedule.PharmacyId,schedule.DayOfWeek,schedule.OpenTime,schedule.ClosedTime);

        return (await GetByPhramacyIdAsync(schedule.PharmacyId,schedule.Id,ct))!;
    }
        

    public  Task<PharmacyScheduleResponse?> GetByPhramacyIdAsync(int phamrmacyId,int id,CancellationToken ct) =>
    context.PharmaciesSchedules.AsNoTracking()
   .Where(ps => ps.PharmacyId == phamrmacyId && ps.Id == id)
   .Select(ps => new PharmacyScheduleResponse(
    ps.Id,ps.PharmacyId,ps.DayOfWeek,ps.OpenTime,ps.ClosedTime,ps.ISClosed
   )).FirstOrDefaultAsync(ct);



}