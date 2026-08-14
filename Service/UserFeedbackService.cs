


using Microsoft.EntityFrameworkCore;
using PmfApi.Data;
using PmfApi.Dto;
using PmfApi.Entities;

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




}