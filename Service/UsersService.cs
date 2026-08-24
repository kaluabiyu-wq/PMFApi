

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

public async Task<PagedResponse<UserResponse>> GetUserAsync(PagedRequest request, CancellationToken ct)
{
    IQueryable<User> query = context.Users.AsNoTracking();

    if (!string.IsNullOrWhiteSpace(request.Search))
    {
        query = query.Where(u => EF.Functions.ILike(u.FullName, "$%{request.Search}%")
                               || EF.Functions.ILike(u.Email, $"%{request.Search}%"));
    }

    var totalCount = await query.CountAsync(ct);

    IOrderedQueryable<User> sortedQuery = request.OrderBy switch
    {
        "FullName" => request.Descending
            ? query.OrderByDescending(u => u.FullName)
            : query.OrderBy(u => u.FullName),
        "Email" => request.Descending
            ? query.OrderByDescending(c => c.Email)
            : query.OrderBy(c => c.Email),
        _ => request.Descending
            ? query.OrderByDescending(u => u.IsActive)
            : query.OrderBy(u => u.IsActive)
    };

    var items = await sortedQuery
        .Skip((request.Page - 1) * request.PageSize)
        .Take(request.PageSize)
        .Select(u => new UserResponse( u.Id,u.FullName,
        u.Email,u.Password,u.LocationId,u.IsActive,
        u.RoleId,u.CreatedAt))
        .ToListAsync(ct);

    return new PagedResponse<UserResponse>
    {
        Items = items,
        TotalCount = totalCount,
        Page = request.Page,
        PageSize = request.PageSize
    };
}
}

