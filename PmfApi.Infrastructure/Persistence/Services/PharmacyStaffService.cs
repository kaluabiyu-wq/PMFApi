using Microsoft.EntityFrameworkCore;
using PmfApi.Infrastructure.Persistence;
using PmfApi.Application.Dtos;
using PmfApi.Domain.Entities;
using PmfApi.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace PmfApi.Infrastructure.Persistence.Services;

public class PharmacyStaffService(PmfDbContext context, ILogger<PharmacyStaffService> logger)
: IPharmacyStaffService
{
    public async Task<PharmacyStaffResponse> CreateAsync(int pharmacyId, PharmacyStaffRequest request, CancellationToken ct)
    {
        var staff = new PharmacyStaff
        {
            PharmacyId = pharmacyId,
            UserId = request.UserId,
            Position = request.Position,
            IsActive = request.IsActive,
            AssignedAt = DateTime.UtcNow,
        };

        context.PharmacyStaff.Add(staff);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("Assigned User {UserId} to Pharmacy {PharmacyId} as {Position} (PharmacyStaffId {PharmacyStaffId})",
            staff.UserId, staff.PharmacyId, staff.Position, staff.Id);

        return (await GetByPharmacyIdAsync(staff.PharmacyId, staff.Id, ct))!;
    }

    public Task<PharmacyStaffResponse?> GetByPharmacyIdAsync(int pharmacyId, int id, CancellationToken ct) =>
    context.PharmacyStaff.AsNoTracking()
    .Where(ps => ps.Id == id && ps.PharmacyId == pharmacyId)
    .Select(ps => new PharmacyStaffResponse(
        ps.Id, ps.UserId, ps.PharmacyId, ps.Position, ps.IsActive, ps.AssignedAt
    )).FirstOrDefaultAsync(ct);

    public Task<List<PharmacyStaffResponse>> GetByPharmacyAsync(int pharmacyId, CancellationToken ct) =>
    context.PharmacyStaff.AsNoTracking()
    .Where(ps => ps.PharmacyId == pharmacyId)
    .OrderBy(ps => ps.Id)
    .Select(ps => new PharmacyStaffResponse(
        ps.Id, ps.UserId, ps.PharmacyId, ps.Position, ps.IsActive, ps.AssignedAt
    )).ToListAsync(ct);

    public Task<bool> IsAuthorizedForPharmacyAsync(int userId, int pharmacyId, CancellationToken ct) =>
    context.PharmacyStaff.AsNoTracking()
    .AnyAsync(ps => ps.UserId == userId && ps.PharmacyId == pharmacyId && ps.IsActive, ct);

    public async Task<PharmacyStaffResponse?> DeactivateAsync(int pharmacyId, int id, CancellationToken ct)
    {
        var staff = await context.PharmacyStaff
            .FirstOrDefaultAsync(ps => ps.Id == id && ps.PharmacyId == pharmacyId, ct);

        if (staff is null) return null;

        staff.IsActive = false;
        await context.SaveChangesAsync(ct);

        logger.LogInformation("Deactivated PharmacyStaff {PharmacyStaffId} for Pharmacy {PharmacyId}",
            staff.Id, staff.PharmacyId);

        return await GetByPharmacyIdAsync(pharmacyId, id, ct);
    }
}
