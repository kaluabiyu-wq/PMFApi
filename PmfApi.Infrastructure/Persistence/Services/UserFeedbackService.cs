using Microsoft.EntityFrameworkCore;
using PmfApi.Infrastructure.Persistence;
using PmfApi.Application.Dtos;
using PmfApi.Domain.Entities;
using PmfApi.Application.Interfaces;
using Microsoft.Extensions.Logging;


namespace PmfApi.Infrastructure.Persistence.Services;
public class UserFeedBackService(PmfDbContext context,ILogger <UserFeedBackService> logger)
: IUserFeedBackService
{
    
  
    public async Task<UserFeedBackResponse> CreateAsync (int userId,UserFeedBackRequest request,CancellationToken ct)
    {
        var feedback = new UserFeedback
        {
          UserId = userId,
          InventoryId = request.InventoryId,
          PharmacyId = request.PharmacyId,
          WasMedicineAvailable = request.WasMedicineAvailable,
          SubmittedAt = DateTime.UtcNow  
        };
        context.UserFeedbacks.Add(feedback);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("Created User Feedback {UserId} {InventoryId} {PharamacyId} {WasMedicineAvailable}",
            feedback.Id,feedback.InventoryId,feedback.PharmacyId,feedback.WasMedicineAvailable);

        return (await GetByUserIdAsync(feedback.UserId,feedback.InventoryId,ct))!;

    }

   public Task<UserFeedBackResponse?> GetByUserIdAsync(int userId,int inventoryId,CancellationToken ct) =>
    context.UserFeedbacks.AsNoTracking()
    .Where(f => f.UserId == userId && f.InventoryId == inventoryId)
    .Select(f => new UserFeedBackResponse(
       f.Id,f.UserId,f.InventoryId,f.PharmacyId,
       f.WasMedicineAvailable,f.Comments,f.SubmittedAt
    )).FirstOrDefaultAsync(ct);

      public async Task<PagedResponse<UserFeedBackResponse>> GetAllAsync(PagedRequest request, CancellationToken ct)
    {
        IQueryable<UserFeedback> query = context.UserFeedbacks.AsNoTracking();
 
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(f => f.Comments != null && EF.Functions.ILike(f.Comments, $"%{request.Search}%"));
        }
 
        var totalCount = await query.CountAsync(ct);
 
        IOrderedQueryable<UserFeedback> sortedQuery = request.OrderBy switch
        {
            "PharmacyId" => request.Descending
                ? query.OrderByDescending(f => f.PharmacyId)
                : query.OrderBy(f => f.PharmacyId),
            "InventoryId" => request.Descending
                ? query.OrderByDescending(f => f.InventoryId)
                : query.OrderBy(f => f.InventoryId),
            _ => request.Descending
                ? query.OrderByDescending(f => f.SubmittedAt)
                : query.OrderBy(f => f.SubmittedAt)
        };
 
        var items = await sortedQuery
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(f => new UserFeedBackResponse(
                f.Id, f.UserId, f.InventoryId, f.PharmacyId,
                f.WasMedicineAvailable, f.Comments, f.SubmittedAt))
            .ToListAsync(ct);
 
        return new PagedResponse<UserFeedBackResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }


}