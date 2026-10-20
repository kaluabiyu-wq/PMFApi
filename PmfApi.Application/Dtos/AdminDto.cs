using System.ComponentModel.DataAnnotations;
using PmfApi.Domain.Entities;

namespace PmfApi.Application.Dtos;



public record AdminUserQuery
{
    private int _pageSize = 20;

    public int Page { get; init; } = 1;

    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value < 1 ? 20 : value > 50 ? 50 : value;
    }


    public string? Search { get; init; }
    public int? RoleId { get; init; }

    /// <summary>Omit to list every account that has not been deleted.</summary>
    public AccountStatus? Status { get; init; }

    /// <summary>FullName, Email, LastLoginAt or CreatedAt (default).</summary>
    public string? OrderBy { get; init; }
    public bool Descending { get; init; } = true;
}

public record AdminUserResponse(
    int Id,
    string FullName,
    string Email,
    int RoleId,
    string RoleName,
    AccountStatus Status,
    DateTime CreatedAt,
    DateTime? LastLoginAt,
    DateTime? SuspendedAt,
    string? SuspensionReason,
    int LocationId,
    string LocationLabel,
    int? PharmacyId,
    string? PharmacyName
);

public record AdminUserDetailResponse(
    AdminUserResponse User,
    int OrderCount,
    int OpenOrderCount,
    int ReviewCount,
    int FavoriteCount
);

public record AdminUserCreateRequest
{
    [Required, MinLength(2), MaxLength(100)]
    public required string FullName { get; init; }

    [Required, EmailAddress, MaxLength(200)]
    public required string Email { get; init; }

    /// <summary>Leave empty to generate a one-time temporary password.</summary>
    [MinLength(8), MaxLength(100)]
    public string? Password { get; init; }

    [Required]
    public required int RoleId { get; init; }

    [Required]
    public required int LocationId { get; init; }

    /// <summary>Also email the person a link to choose their own password.</summary>
    public bool SendResetEmail { get; init; }
}

public record AdminUserCreatedResponse(
    AdminUserDetailResponse Detail,
    string? TemporaryPassword,
    bool EmailSent
);

public record AdminUserUpdateRequest
{
    [Required, MinLength(2), MaxLength(100)]
    public required string FullName { get; init; }

    [Required, EmailAddress, MaxLength(200)]
    public required string Email { get; init; }
}

public record AdminRoleChangeRequest
{
    [Required]
    public required int RoleId { get; init; }
}

public record AdminStatusRequest
{
    /// <summary>Active (restore), Suspended or Inactive. Deleted has its own endpoint.</summary>
    [Required]
    public required AccountStatus Status { get; init; }

    [MaxLength(300)]
    public string? Reason { get; init; }
}

public record AdminDeleteRequest
{
    /// <summary>Must equal the account's email; guards against deleting the wrong row.</summary>
    [Required]
    public required string ConfirmEmail { get; init; }
}

public record TemporaryPasswordResponse(string TemporaryPassword);

public record SendResetEmailResponse(bool EmailSent, string Message);

// ---------- Roles ----------

public record RoleSummaryResponse(
    int Id,
    string Name,
    string? Description,
    int UserCount,
    IReadOnlyList<string> Permissions
);

// ---------- Public auth flows ----------

public record RegisterPatientRequest
{
    [Required, MinLength(2), MaxLength(100)]
    public required string FullName { get; init; }

    [Required, EmailAddress, MaxLength(200)]
    public required string Email { get; init; }

    [Required, MinLength(8), MaxLength(100)]
    public required string Password { get; init; }

    [Required]
    public required int LocationId { get; init; }
}

public record ForgotPasswordRequest
{
    [Required, EmailAddress]
    public required string Email { get; init; }
}

public record ResetPasswordRequest
{
    [Required, EmailAddress]
    public required string Email { get; init; }

    [Required]
    public required string Token { get; init; }

    [Required, MinLength(8), MaxLength(100)]
    public required string NewPassword { get; init; }
}

// ---------- Dashboard ----------

public record CountByName(string Name, int Count);
public record DailyCount(string Date, int Count);

public record AdminOverviewResponse(
    int TotalUsers,
    int NewUsersLast7Days,
    int NewUsersLast30Days,
    int ActiveUsers,
    int SuspendedUsers,
    int InactiveUsers,
    IReadOnlyList<CountByName> UsersByRole,
    IReadOnlyList<DailyCount> SignupsLast14Days,
    int TotalPharmacies,
    int VerifiedPharmacies,
    int UnverifiedPharmacies,
    int InactivePharmacies,
    int MedicinesInCatalogue,
    int InventoryListings,
    int PendingDocuments,
    int RejectedDocuments,
    int PendingPrescriptions,
    int PendingOrders,
    IReadOnlyList<AuditLogResponse> RecentActivity
);

public record AuditLogResponse(
    int Id,
    int ActorUserId,
    string ActorName,
    string Action,
    string TargetType,
    int TargetId,
    string? Detail,
    DateTime CreatedAt
);

public record AuditQuery
{
    private int _pageSize = 20;

    public int Page { get; init; } = 1;

    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value < 1 ? 20 : value > 50 ? 50 : value;
    }

    public string? Action { get; init; }
}

public record SystemAlert(string Severity, string Message);

public record SystemHealthResponse(
    string Status,
    DateTime CheckedAt,
    string Version,
    string Environment,
    long UptimeSeconds,
    long MemoryMb,
    bool DatabaseOk,
    long DatabaseLatencyMs,
    bool EmailConfigured,
    IReadOnlyList<SystemAlert> Alerts
);