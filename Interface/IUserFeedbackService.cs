using PmfApi.Dto;
using PmfApi.Entities;

public interface IUserFeedBackService
{
    Task<UserFeedBackResponse> CreateAsync (int userId,UserFeedBackRequest request,CancellationToken ct);

   Task<UserFeedBackResponse?> GetByUserIdAsync(int userId,int inventoryId,CancellationToken ct);


   
}