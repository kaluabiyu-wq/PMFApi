

using Microsoft.EntityFrameworkCore;
using PmfApi.Data;
using PmfApi.Dto;
using PmfApi.Entities;

namespace PmfApi.Service;
public class UserService(PmfDbContext context,ILogger<UserService> logger)
: IUserService
{
    
public async Task<UserResponse> CreateAsync(UserRequest request,CancellationToken ct)
    {
         var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            RoleId = request.RoleId,
             Password = request.Password,
            LocationId = request.LocationId

            
        };
        context.Users.Add(user);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("Created User {UserId} {FullName} {Email} {Password}",
            user.Id,user.FullName,user.Email,user.Password);

        return (await GetByeEmailAsync(user.Email,ct))!;
    }
        
    


public Task<UserResponse?> GetByeEmailAsync(string email, CancellationToken ct) =>
       context.Users.AsNoTracking()
       .Where(u => u.Email == email)
       .Select( u=>  new UserResponse(
        u.Id,u.FullName,u.Email,u.Password,
        u.LocationId,u.IsActive,u.RoleId
      ,u.CreatedAt)).FirstOrDefaultAsync(ct);

}

