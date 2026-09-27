using Microsoft.AspNetCore.Identity;
using PmfApi.Application.Dtos;
using PmfApi.Application.Interfaces;
using PmfApi.Domain.Entities;

namespace PmfApi.Infrastructure.Persistence.Services;

public class AuthService(
    IUserService userService,
    IPasswordHasher<User> passwordHasher,
    ITokenService tokenService) : IAuthService
{
       private static readonly User DummyUserForTiming = new()
    {
        FullName = string.Empty,
        Email = string.Empty,
        Password = string.Empty,
        RoleId = 0,
    };

    private static readonly string DummyPasswordHash =
        new PasswordHasher<User>().HashPassword(DummyUserForTiming, "not-a-real-password");

    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var user = await userService.GetUserEntityByEmailAsync(request.Email, ct);

        if (user is null)
        {
           
            passwordHasher.VerifyHashedPassword(DummyUserForTiming, DummyPasswordHash, request.Password);
            return null;
        }

        var verification = passwordHasher.VerifyHashedPassword(user, user.Password, request.Password);

        if (verification == PasswordVerificationResult.Failed)
        {
            return null;
        }

       
        var (token, expiresAt) = tokenService.GenerateToken(user);

        return new LoginResponse(token, expiresAt, user.Id, user.FullName, user.Role.Name);
    }
}