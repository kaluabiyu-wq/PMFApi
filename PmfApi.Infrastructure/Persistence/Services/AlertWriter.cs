using Microsoft.EntityFrameworkCore;
using PmfApi.Domain.Entities;

namespace PmfApi.Infrastructure.Persistence.Services;


internal static class AlertWriter
{
    public static void ForUser(
        PmfDbContext context, int userId, AlertEventType eventType,
        string referenceTable, int referenceId, string message)
    {
        context.Alerts.Add(new Alert
        {
            UserId = userId,
            EventType = eventType,
            ReferenceTable = referenceTable,
            ReferenceId = referenceId,
            Message = message,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
        });
    }

   
    public static async Task ForPharmacyStaffAsync(
        PmfDbContext context, int pharmacyId, AlertEventType eventType,
        string referenceTable, int referenceId, string message, CancellationToken ct)
    {
        var staffUserIds = await context.PharmacyStaff.AsNoTracking()
            .Where(s => s.PharmacyId == pharmacyId && s.IsActive && s.User.IsActive)
            .Select(s => s.UserId)
            .Distinct()
            .ToListAsync(ct);

        foreach (var userId in staffUserIds)
            ForUser(context, userId, eventType, referenceTable, referenceId, message);
    }
}