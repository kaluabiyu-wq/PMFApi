using PmfApi.Domain.Entities;

namespace PmfApi.Application.Interfaces;

public interface ITokenService
{    (string Token, DateTime ExpiresAt) GenerateToken(User user);
}