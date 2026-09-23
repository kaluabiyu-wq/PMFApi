

namespace PmfApi.Application.Dtos;

public record PharmacyStaffResponse(
    int Id,
    int UserId,
    int PharmacyId,
    string Position,
    bool IsActive,
    DateTime AssignedAt
);
