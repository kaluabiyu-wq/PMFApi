namespace PmfApi.Application.Dtos;

public record LoginResponse
(
    string Token,
    DateTime ExpiresAt,
    int UserId,
    string FullName,
    string Role
);