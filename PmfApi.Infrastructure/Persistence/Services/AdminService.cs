using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using PmfApi.Application.Abstractions;
using PmfApi.Application.Dtos;
using PmfApi.Application.Exceptions;
using PmfApi.Application.Interfaces;
using PmfApi.Domain.Entities;

namespace PmfApi.Infrastructure.Persistence.Services;

public class AdminService(
    PmfDbContext context,
    IPasswordHasher<User> hasher,
    IAccountRecoveryService recovery,
    IEmailSender emailSender,
    IMemoryCache cache,
    ILogger<AdminService> logger) : IAdminService
{
    private const string AdminRole = "SysAdmin";

    private static readonly IReadOnlyDictionary<string, string[]> RolePermissions =
        new Dictionary<string, string[]>
        {
            ["SysAdmin"] =
            [
                "Manage every user account (create, edit, suspend, deactivate, delete)",
                "Assign and revoke roles",
                "Reset passwords and send reset emails",
                "Verify or suspend pharmacies and review their documents",
                "View the activity log and system health",
            ],
            ["PharmacyAdmin"] =
            [
                "Review pharmacy documents and verify pharmacies",
                "Approve inventory changes",
                "Manage the medicine catalogue",
                "View every pharmacy's orders and feedback",
            ],
            ["Pharmacy"] =
            [
                "Manage the pharmacy's own information and documents",
                "Manage inventory, schedule and staff",
                "Handle orders and verify prescriptions",
            ],
            ["Patient"] =
            [
                "Search medicines and pharmacies",
                "Place orders and upload prescriptions",
                "Write reviews and keep favourites",
                "Manage their own account",
            ],
        };



    public async Task<PagedResponse<AdminUserResponse>> ListUsersAsync(AdminUserQuery q, CancellationToken ct)
    {
        IQueryable<User> query = context.Users.AsNoTracking();

        query = q.Status switch
        {
            AccountStatus.Deleted => query.Where(u => u.DeletedAt != null),
            AccountStatus.Inactive => query.Where(u => u.DeletedAt == null && !u.IsActive),
            AccountStatus.Suspended => query.Where(u => u.DeletedAt == null && u.IsActive && u.SuspendedAt != null),
            AccountStatus.Active => query.Where(u => u.DeletedAt == null && u.IsActive && u.SuspendedAt == null),
            _ => query.Where(u => u.DeletedAt == null),
        };

        if (q.RoleId is { } roleId)
            query = query.Where(u => u.RoleId == roleId);

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var pattern = $"%{q.Search.Trim()}%";
            query = query.Where(u => EF.Functions.ILike(u.FullName, pattern) || EF.Functions.ILike(u.Email, pattern));
        }

        var total = await query.CountAsync(ct);

        IOrderedQueryable<User> ordered = (q.OrderBy, q.Descending) switch
        {
            ("FullName", true) => query.OrderByDescending(u => u.FullName),
            ("FullName", false) => query.OrderBy(u => u.FullName),
            ("Email", true) => query.OrderByDescending(u => u.Email),
            ("Email", false) => query.OrderBy(u => u.Email),
            ("LastLoginAt", true) => query.OrderByDescending(u => u.LastLoginAt),
            ("LastLoginAt", false) => query.OrderBy(u => u.LastLoginAt),
            (_, false) => query.OrderBy(u => u.CreatedAt),
            _ => query.OrderByDescending(u => u.CreatedAt),
        };

        var items = await Project(ordered.Skip((q.Page - 1) * q.PageSize).Take(q.PageSize)).ToListAsync(ct);

        return new PagedResponse<AdminUserResponse>
        {
            Items = items,
            TotalCount = total,
            Page = q.Page,
            PageSize = q.PageSize,
        };
    }

    public async Task<AdminUserDetailResponse?> GetUserAsync(int id, CancellationToken ct)
    {
        var user = await Project(context.Users.AsNoTracking().Where(u => u.Id == id)).FirstOrDefaultAsync(ct);
        if (user is null) return null;

        return new AdminUserDetailResponse(
            user,
            await context.Orders.CountAsync(o => o.UserId == id, ct),
            await context.Orders.CountAsync(o => o.UserId == id
                && (o.Status == OrderStatus.Pending || o.Status == OrderStatus.Confirmed), ct),
            await context.Reviews.CountAsync(r => r.UserId == id, ct),
            await context.Favorites.CountAsync(f => f.UserId == id, ct));
    }

    public async Task<AdminUserCreatedResponse> CreateUserAsync(int actorId, AdminUserCreateRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim();

        if (await context.Users.AnyAsync(u => u.Email == email, ct))
            throw new DuplicateEmailException();
        if (!await context.Roles.AnyAsync(r => r.Id == request.RoleId, ct))
            throw new RoleNotFoundException();
        if (!await context.Locations.AnyAsync(l => l.Id == request.LocationId, ct))
            throw new LocationNotFoundException();

        var password = string.IsNullOrEmpty(request.Password) ? GeneratePassword() : request.Password;

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            Password = string.Empty,
            RoleId = request.RoleId,
            LocationId = request.LocationId,
        };
        user.Password = hasher.HashPassword(user, password);

        context.Users.Add(user);
        await context.SaveChangesAsync(ct);

        var roleName = await context.Roles.Where(r => r.Id == user.RoleId).Select(r => r.Name).FirstAsync(ct);
        await AuditAsync(actorId, "UserCreated", user.Id, $"Created a {roleName} account.", ct);
        await context.SaveChangesAsync(ct);

        var emailSent = request.SendResetEmail && await recovery.SendResetLinkAsync(user, ct);

        logger.LogInformation("Admin {ActorId} created user {UserId} as {Role}", actorId, user.Id, roleName);

        return new AdminUserCreatedResponse(
            (await GetUserAsync(user.Id, ct))!,
            string.IsNullOrEmpty(request.Password) ? password : null,
            emailSent);
    }

    public async Task<AdminUserDetailResponse?> UpdateUserAsync(int actorId, int id, AdminUserUpdateRequest request, CancellationToken ct)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null, ct);
        if (user is null) return null;

        var email = request.Email.Trim();
        var name = request.FullName.Trim();

        if (email != user.Email && await context.Users.AnyAsync(u => u.Id != id && u.Email == email, ct))
            throw new DuplicateEmailException();

        var changes = new List<string>();
        if (name != user.FullName) changes.Add("name");
        if (email != user.Email) changes.Add("email");

        user.FullName = name;
        user.Email = email;

        if (changes.Count > 0)
            await AuditAsync(actorId, "UserUpdated", id, $"Changed {string.Join(" and ", changes)}.", ct);

        await context.SaveChangesAsync(ct);
        return await GetUserAsync(id, ct);
    }

    public async Task<AdminUserDetailResponse?> ChangeRoleAsync(int actorId, int id, int roleId, CancellationToken ct)
    {
        var user = await context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null, ct);
        if (user is null) return null;

        var newRole = await context.Roles.FirstOrDefaultAsync(r => r.Id == roleId, ct)
            ?? throw new RoleNotFoundException();

        if (newRole.Id == user.RoleId) return await GetUserAsync(id, ct);

        if (id == actorId)
            throw new AdminRuleException("You cannot change your own role. Ask another administrator to do it.");

        // Taking someone out of the administrator role removes administrator access like any other lock-out.
        if (user.Role.Name == AdminRole && newRole.Name != AdminRole)
            await EnsureAnotherAdminAsync(user, ct);

        var oldName = user.Role.Name;
        user.RoleId = newRole.Id;

        await AuditAsync(actorId, "RoleChanged", id, $"Role changed from {oldName} to {newRole.Name}.", ct);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("Admin {ActorId} changed user {UserId} role {Old} -> {New}", actorId, id, oldName, newRole.Name);
        return await GetUserAsync(id, ct);
    }

    public async Task<AdminUserDetailResponse?> SetStatusAsync(
        int actorId, int id, AccountStatus status, string? reason, CancellationToken ct)
    {
        if (status == AccountStatus.Deleted)
            throw new AdminRuleException("Use the delete action to delete an account.");

        var user = await context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null, ct);
        if (user is null) return null;

        var current = CurrentStatus(user);
        if (current == status) return await GetUserAsync(id, ct);

        // Suspending or deactivating is the only way an administrator can lose access.
        if (status is AccountStatus.Suspended or AccountStatus.Inactive)
        {
            if (id == actorId)
                throw new AdminRuleException("You cannot suspend or deactivate your own account. Ask another administrator.");
            if (user.Role.Name == AdminRole && current == AccountStatus.Active)
                await EnsureAnotherAdminAsync(user, ct);
        }

        string action;
        string detail;
        switch (status)
        {
            case AccountStatus.Suspended:
                user.IsActive = true;
                user.SuspendedAt = DateTime.UtcNow;
                user.SuspensionReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
                action = "UserSuspended";
                detail = user.SuspensionReason is null ? "Suspended." : $"Suspended: {user.SuspensionReason}";
                break;
            case AccountStatus.Inactive:
                user.IsActive = false;
                user.SuspendedAt = null;
                user.SuspensionReason = null;
                action = "UserDeactivated";
                detail = "Deactivated.";
                break;
            default: // Active = restore
                user.IsActive = true;
                user.SuspendedAt = null;
                user.SuspensionReason = null;
                action = "UserRestored";
                detail = $"Restored from {current}.";
                break;
        }

        await AuditAsync(actorId, action, id, detail, ct);
        await context.SaveChangesAsync(ct);
        cache.Remove(UserCacheKeys.CanUseApi(id));

        logger.LogInformation("Admin {ActorId} set user {UserId} {From} -> {To}", actorId, id, current, status);
        return await GetUserAsync(id, ct);
    }

    public async Task<bool> DeleteUserAsync(int actorId, int id, string confirmEmail, CancellationToken ct)
    {
        var user = await context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null, ct);
        if (user is null) return false;

        if (!string.Equals(confirmEmail.Trim(), user.Email, StringComparison.OrdinalIgnoreCase))
            throw new AdminRuleException("The email you typed does not match this account, so nothing was deleted.");

        if (id == actorId)
            throw new AdminRuleException("You cannot delete your own account. Ask another administrator.");

        if (user.Role.Name == AdminRole && CurrentStatus(user) == AccountStatus.Active)
            await EnsureAnotherAdminAsync(user, ct);

        if (await context.Orders.AnyAsync(o => o.UserId == id
                && (o.Status == OrderStatus.Pending || o.Status == OrderStatus.Confirmed), ct))
            throw new AdminRuleException("This account has pending or confirmed orders. Wait until they are completed or cancelled.");

        var roleName = user.Role.Name;

        // Retention: the row stays (orders, reviews and prescriptions point at it) but nothing personal remains.
        user.FullName = "Deleted user";
        user.Email = $"deleted-{user.Id}@deleted.invalid";
        user.Password = hasher.HashPassword(user, Convert.ToHexString(RandomNumberGenerator.GetBytes(24)));
        user.IsActive = false;
        user.SuspendedAt = null;
        user.SuspensionReason = null;
        user.PasswordResetTokenHash = null;
        user.PasswordResetExpiresAt = null;
        user.DeletedAt = DateTime.UtcNow;

        // Data that only matters to the person is removed outright.
        context.DeviceTokens.RemoveRange(await context.DeviceTokens.Where(d => d.UserId == id).ToListAsync(ct));
        context.Favorites.RemoveRange(await context.Favorites.Where(f => f.UserId == id).ToListAsync(ct));
        context.Searches.RemoveRange(await context.Searches.Where(s => s.UserId == id).ToListAsync(ct));
        context.Alerts.RemoveRange(await context.Alerts.Where(a => a.UserId == id).ToListAsync(ct));

        // A deleted pharmacist must not keep working rights at a pharmacy.
        foreach (var staff in await context.PharmacyStaff.Where(s => s.UserId == id).ToListAsync(ct))
            staff.IsActive = false;

        // Only the id and role are recorded: the email is deliberately not copied into the log.
        await AuditAsync(actorId, "UserDeleted", id,
            $"Deleted a {roleName} account. Personal details anonymised; orders, reviews and prescriptions kept.", ct);

        await context.SaveChangesAsync(ct);
        cache.Remove(UserCacheKeys.CanUseApi(id));

        logger.LogInformation("Admin {ActorId} deleted user {UserId}", actorId, id);
        return true;
    }

    public async Task<string?> ResetPasswordAsync(int actorId, int id, CancellationToken ct)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null, ct);
        if (user is null) return null;

        var password = GeneratePassword();
        user.Password = hasher.HashPassword(user, password);
        user.PasswordResetTokenHash = null; // any emailed link issued earlier no longer works
        user.PasswordResetExpiresAt = null;

        await AuditAsync(actorId, "PasswordReset", id, "Set a temporary password.", ct);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("Admin {ActorId} reset the password of user {UserId}", actorId, id);
        return password;
    }

    public async Task<bool?> SendResetEmailAsync(int actorId, int id, CancellationToken ct)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null, ct);
        if (user is null) return null;

        var sent = await recovery.SendResetLinkAsync(user, ct);

        await AuditAsync(actorId, "ResetEmailSent", id, sent ? "Reset email sent." : "Reset email could not be sent.", ct);
        await context.SaveChangesAsync(ct);
        return sent;
    }

    // ---------- Roles ----------

    public async Task<IReadOnlyList<RoleSummaryResponse>> GetRolesAsync(CancellationToken ct)
    {
        var roles = await context.Roles.AsNoTracking()
            .OrderBy(r => r.Id)
            .Select(r => new { r.Id, r.Name, r.Description, Count = r.Users.Count(u => u.DeletedAt == null) })
            .ToListAsync(ct);

        return roles
            .Select(r => new RoleSummaryResponse(
                r.Id, r.Name, r.Description, r.Count,
                RolePermissions.TryGetValue(r.Name, out var p) ? p : []))
            .ToList();
    }

    // ---------- Dashboard ----------

    public async Task<AdminOverviewResponse> GetOverviewAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var live = context.Users.AsNoTracking().Where(u => u.DeletedAt == null);

        var byRole = await context.Roles.AsNoTracking().OrderBy(r => r.Id)
            .Select(r => new CountByName(r.Name, r.Users.Count(u => u.DeletedAt == null)))
            .ToListAsync(ct);

        var since = now.Date.AddDays(-13);
        var perDay = await live.Where(u => u.CreatedAt >= since)
            .GroupBy(u => u.CreatedAt.Date)
            .Select(g => new { Day = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var signups = Enumerable.Range(0, 14)
            .Select(i => since.AddDays(i))
            .Select(day => new DailyCount(
                day.ToString("yyyy-MM-dd"),
                perDay.FirstOrDefault(x => x.Day.Date == day)?.Count ?? 0))
            .ToList();

        var recent = await RecentAuditAsync(10, ct);

        return new AdminOverviewResponse(
            TotalUsers: await live.CountAsync(ct),
            NewUsersLast7Days: await live.CountAsync(u => u.CreatedAt >= now.AddDays(-7), ct),
            NewUsersLast30Days: await live.CountAsync(u => u.CreatedAt >= now.AddDays(-30), ct),
            ActiveUsers: await live.CountAsync(u => u.IsActive && u.SuspendedAt == null, ct),
            SuspendedUsers: await live.CountAsync(u => u.IsActive && u.SuspendedAt != null, ct),
            InactiveUsers: await live.CountAsync(u => !u.IsActive, ct),
            UsersByRole: byRole,
            SignupsLast14Days: signups,
            TotalPharmacies: await context.Pharmacies.CountAsync(ct),
            VerifiedPharmacies: await context.Pharmacies.CountAsync(p => p.IsActive && p.IsVerified, ct),
            UnverifiedPharmacies: await context.Pharmacies.CountAsync(p => p.IsActive && !p.IsVerified, ct),
            InactivePharmacies: await context.Pharmacies.CountAsync(p => !p.IsActive, ct),
            MedicinesInCatalogue: await context.Medicines.CountAsync(ct),
            InventoryListings: await context.Inventories.CountAsync(ct),
            PendingDocuments: await context.PharmacyDocuments.CountAsync(d => d.ReviewStatus == ReviewStatus.Pending, ct),
            RejectedDocuments: await context.PharmacyDocuments.CountAsync(d => d.ReviewStatus == ReviewStatus.Rejected, ct),
            PendingPrescriptions: await context.Prescriptions.CountAsync(p => p.VerificationStatus == VerificationStatus.Pending, ct),
            PendingOrders: await context.Orders.CountAsync(o => o.Status == OrderStatus.Pending, ct),
            RecentActivity: recent);
    }

    public async Task<SystemHealthResponse> GetHealthAsync(CancellationToken ct)
    {
        var watch = Stopwatch.StartNew();
        bool dbOk;
        try { dbOk = await context.Database.CanConnectAsync(ct); }
        catch { dbOk = false; }
        watch.Stop();

        var alerts = new List<SystemAlert>();

        if (!dbOk)
            alerts.Add(new("Critical", "The database cannot be reached."));
        else if (watch.ElapsedMilliseconds > 500)
            alerts.Add(new("Warning", $"The database is slow to respond ({watch.ElapsedMilliseconds} ms)."));

        if (dbOk)
        {
            var unverified = await context.Pharmacies.CountAsync(p => p.IsActive && !p.IsVerified, ct);
            if (unverified > 0)
                alerts.Add(new("Warning", $"{unverified} pharmacy(ies) are waiting for verification."));

            var oldPending = await context.PharmacyDocuments.CountAsync(
                d => d.ReviewStatus == ReviewStatus.Pending && d.UploadedAt < DateTime.UtcNow.AddDays(-3), ct);
            if (oldPending > 0)
                alerts.Add(new("Warning", $"{oldPending} document(s) have been waiting for review for more than 3 days."));

            var prescriptions = await context.Prescriptions.CountAsync(p => p.VerificationStatus == VerificationStatus.Pending, ct);
            if (prescriptions > 0)
                alerts.Add(new("Info", $"{prescriptions} prescription(s) are waiting for verification."));

            var suspended = await context.Users.CountAsync(u => u.DeletedAt == null && u.IsActive && u.SuspendedAt != null, ct);
            if (suspended > 0)
                alerts.Add(new("Info", $"{suspended} account(s) are currently suspended."));

            var admins = await context.Users.CountAsync(
                u => u.Role.Name == AdminRole && u.IsActive && u.SuspendedAt == null && u.DeletedAt == null, ct);
            if (admins < 2)
                alerts.Add(new("Warning", "Only one active administrator exists. Create a second so you cannot be locked out."));
        }

        if (!emailSender.IsConfigured)
            alerts.Add(new("Warning", "Email is not configured, so password-reset emails cannot be sent."));

        var status = !dbOk ? "Unhealthy"
            : watch.ElapsedMilliseconds > 500 ? "Degraded"
            : "Healthy";

        var process = Process.GetCurrentProcess();

        return new SystemHealthResponse(
            Status: status,
            CheckedAt: DateTime.UtcNow,
            Version: Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "unknown",
            Environment: Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
            UptimeSeconds: (long)(DateTime.Now - process.StartTime).TotalSeconds,
            MemoryMb: process.WorkingSet64 / (1024 * 1024),
            DatabaseOk: dbOk,
            DatabaseLatencyMs: watch.ElapsedMilliseconds,
            EmailConfigured: emailSender.IsConfigured,
            Alerts: alerts);
    }

    public async Task<PagedResponse<AuditLogResponse>> GetAuditAsync(AuditQuery q, CancellationToken ct)
    {
        var query = context.AuditLogs.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q.Action))
            query = query.Where(a => a.Action == q.Action);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.Id)
            .Skip((q.Page - 1) * q.PageSize).Take(q.PageSize)
            .Select(a => new AuditLogResponse(a.Id, a.ActorUserId, a.ActorName, a.Action, a.TargetType, a.TargetId, a.Detail, a.CreatedAt))
            .ToListAsync(ct);

        return new PagedResponse<AuditLogResponse>
        {
            Items = items,
            TotalCount = total,
            Page = q.Page,
            PageSize = q.PageSize,
        };
    }

    // ---------- Helpers ----------

    private IQueryable<AdminUserResponse> Project(IQueryable<User> query) =>
        query.Select(u => new AdminUserResponse(
            u.Id, u.FullName, u.Email, u.RoleId, u.Role.Name,
            u.DeletedAt != null ? AccountStatus.Deleted
                : !u.IsActive ? AccountStatus.Inactive
                : u.SuspendedAt != null ? AccountStatus.Suspended
                : AccountStatus.Active,
            u.CreatedAt, u.LastLoginAt, u.SuspendedAt, u.SuspensionReason,
            u.LocationId, u.Location.Label,
            context.PharmacyStaff.Where(s => s.UserId == u.Id).Select(s => (int?)s.PharmacyId).FirstOrDefault(),
            context.PharmacyStaff.Where(s => s.UserId == u.Id).Select(s => s.Pharmacy.Name).FirstOrDefault()));

    private static AccountStatus CurrentStatus(User u) =>
        u.DeletedAt != null ? AccountStatus.Deleted
        : !u.IsActive ? AccountStatus.Inactive
        : u.SuspendedAt != null ? AccountStatus.Suspended
        : AccountStatus.Active;

    /// <summary>Refuses an action that would leave the system with no usable administrator.</summary>
    private async Task EnsureAnotherAdminAsync(User target, CancellationToken ct)
    {
        var another = await context.Users.AnyAsync(u =>
            u.Id != target.Id && u.Role.Name == AdminRole
            && u.IsActive && u.SuspendedAt == null && u.DeletedAt == null, ct);

        if (!another)
            throw new AdminRuleException(
                "This is the last active administrator. Create or restore another administrator first.");
    }

    private async Task AuditAsync(int actorId, string action, int targetId, string? detail, CancellationToken ct)
    {
        var actorName = await context.Users.Where(u => u.Id == actorId).Select(u => u.FullName).FirstOrDefaultAsync(ct)
            ?? $"User #{actorId}";

        context.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actorId,
            ActorName = actorName,
            Action = action,
            TargetType = "User",
            TargetId = targetId,
            Detail = detail,
        });
    }

    private Task<List<AuditLogResponse>> RecentAuditAsync(int count, CancellationToken ct) =>
        context.AuditLogs.AsNoTracking()
            .OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.Id)
            .Take(count)
            .Select(a => new AuditLogResponse(a.Id, a.ActorUserId, a.ActorName, a.Action, a.TargetType, a.TargetId, a.Detail, a.CreatedAt))
            .ToListAsync(ct);

    /// <summary>12 characters from an alphabet without look-alikes, always containing each character class.</summary>
    private static string GeneratePassword()
    {
        const string lower = "abcdefghijkmnpqrstuvwxyz";
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string digits = "23456789";
        const string all = lower + upper + digits;

        var chars = new List<char>
        {
            lower[RandomNumberGenerator.GetInt32(lower.Length)],
            upper[RandomNumberGenerator.GetInt32(upper.Length)],
            digits[RandomNumberGenerator.GetInt32(digits.Length)],
        };
        while (chars.Count < 12)
            chars.Add(all[RandomNumberGenerator.GetInt32(all.Length)]);

        // Fisher-Yates so the guaranteed characters are not always first.
        for (var i = chars.Count - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }
        return new string(chars.ToArray());
    }
}