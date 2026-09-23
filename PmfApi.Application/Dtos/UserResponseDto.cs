
namespace PmfApi.Application.Dtos;


public record UserResponse
(
    int Id,
    string FullName,
    string Email,
    string Password,
    int LocationId,
    bool IsActive,
    int RoleId,
    DateTime CreatedAt
);