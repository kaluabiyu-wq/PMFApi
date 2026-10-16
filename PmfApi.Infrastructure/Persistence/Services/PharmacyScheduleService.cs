using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PmfApi.Application.Dtos;
using PmfApi.Application.Interfaces;
using PmfApi.Domain.Entities;

namespace PmfApi.Infrastructure.Persistence.Services;

public class PharmaciesScheduleService(PmfDbContext context, ILogger<PharmaciesScheduleService> logger)
    : IPharmaciesScheduleService
{
    private const string TimeFormat = "HH:mm";

    public static DateTime ToStoredTime(string hhmm)
    {
        var time = TimeOnly.ParseExact(hhmm, TimeFormat, CultureInfo.InvariantCulture);
        return DateTime.SpecifyKind(DateTime.UtcNow.Date + time.ToTimeSpan(), DateTimeKind.Utc);
    }

    private static PharmacyScheduleResponse ToResponse(PharmaciesSchedule s) => new(
        s.Id,
        s.PharmacyId,
        s.DayOfWeek,
        s.OpenTime.ToString(TimeFormat, CultureInfo.InvariantCulture),
        s.ClosedTime.ToString(TimeFormat, CultureInfo.InvariantCulture),
        s.ISClosed);

    public async Task<PharmacyScheduleResponse> CreateAsync(
        int pharmacyId, PharmaciesScheduleRequest request, CancellationToken ct)
    {
        var schedule = new PharmaciesSchedule
        {
            PharmacyId = pharmacyId,
            DayOfWeek = request.DayOfWeek,
            OpenTime = ToStoredTime(request.OpenTime),
            ClosedTime = ToStoredTime(request.ClosedTime),
            ISClosed = request.IsClosed,
        };

        context.PharmaciesSchedules.Add(schedule);
        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Created schedule {ScheduleId} for pharmacy {PharmacyId}: day {DayOfWeek} {Open}-{Close} closed={Closed}",
            schedule.Id, schedule.PharmacyId, schedule.DayOfWeek, request.OpenTime, request.ClosedTime, schedule.ISClosed);

        return ToResponse(schedule);
    }

    public async Task<PharmacyScheduleResponse?> GetByPhramacyIdAsync(int pharmacyId, int id, CancellationToken ct)
    {
        var row = await context.PharmaciesSchedules.AsNoTracking()
            .FirstOrDefaultAsync(ps => ps.PharmacyId == pharmacyId && ps.Id == id, ct);

        return row is null ? null : ToResponse(row);
    }

    public async Task<IReadOnlyList<PharmacyScheduleResponse>> GetWeekAsync(int pharmacyId, CancellationToken ct)
    {
        var rows = await context.PharmaciesSchedules.AsNoTracking()
            .Where(ps => ps.PharmacyId == pharmacyId)
            .OrderBy(ps => ps.DayOfWeek)
            .ToListAsync(ct);

        return rows.Select(ToResponse).ToList();
    }

    public async Task<IReadOnlyList<PharmacyScheduleResponse>> SaveWeekAsync(
        int pharmacyId, IReadOnlyList<PharmaciesScheduleRequest> days, CancellationToken ct)
    {
        var existing = await context.PharmaciesSchedules
            .Where(ps => ps.PharmacyId == pharmacyId)
            .ToListAsync(ct);

        foreach (var day in days)
        {
            var row = existing.FirstOrDefault(r => r.DayOfWeek == day.DayOfWeek);
            if (row is null)
            {
                row = new PharmaciesSchedule { PharmacyId = pharmacyId, DayOfWeek = day.DayOfWeek };
                context.PharmaciesSchedules.Add(row);
                existing.Add(row);
            }

            row.ISClosed = day.IsClosed;
            row.OpenTime = ToStoredTime(day.OpenTime);
            row.ClosedTime = ToStoredTime(day.ClosedTime);
        }

        await context.SaveChangesAsync(ct);

        logger.LogInformation("Saved {Count} schedule days for pharmacy {PharmacyId}", days.Count, pharmacyId);

        return await GetWeekAsync(pharmacyId, ct);
    }
}