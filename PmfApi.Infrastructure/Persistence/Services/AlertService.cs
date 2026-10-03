using Microsoft.EntityFrameworkCore;
using PmfApi.Application.Dtos;
using PmfApi.Application.Interfaces;

namespace PmfApi.Infrastructure.Persistence.Services;

public class AlertService(PmfDbContext context) : IAlertService
{
    public async Task<PagedResponse<AlertResponse>> GetMineAsync(int userId, AlertListQuery query, CancellationToken ct)
    {
        var source = context.Alerts.AsNoTracking().Where(a => a.UserId == userId);

        if (query.IsRead is { } isRead)
            source = source.Where(a => a.IsRead == isRead);

        var page = Math.Max(1, query.Page);
        var total = await source.CountAsync(ct);

        var items = await source
            .OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.Id)
            .Skip((page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(a => new AlertResponse(
                a.Id, a.EventType, a.ReferenceTable, a.ReferenceId, a.Message, a.IsRead, a.CreatedAt))
            .ToListAsync(ct);

        return new PagedResponse<AlertResponse>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = query.PageSize,
        };
    }

    public Task<int> CountUnreadAsync(int userId, CancellationToken ct) =>
        context.Alerts.AsNoTracking().CountAsync(a => a.UserId == userId && !a.IsRead, ct);

    public async Task<bool> MarkReadAsync(int userId, int id, CancellationToken ct)
    {
        
        var rows = await context.Alerts
            .Where(a => a.Id == id && a.UserId == userId && !a.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsRead, true), ct);

        if (rows > 0) return true;

        
        return await context.Alerts.AnyAsync(a => a.Id == id && a.UserId == userId, ct);
    }

    public Task<int> MarkAllReadAsync(int userId, CancellationToken ct) =>
        context.Alerts
            .Where(a => a.UserId == userId && !a.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsRead, true), ct);
}