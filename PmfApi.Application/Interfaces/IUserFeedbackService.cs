
using PmfApi.Application.Dtos;
using PmfApi.Domain.Entities;


namespace PmfApi.Application.Interfaces;

public interface IUserFeedBackService
{
    Task<UserFeedBackResponse> CreateAsync (int userId,UserFeedBackRequest request,CancellationToken ct);

   Task<UserFeedBackResponse?> GetByUserIdAsync(int userId,int inventoryId,CancellationToken ct);

Task<PagedResponse<UserFeedBackResponse>> GetAllAsync(PagedRequest request, CancellationToken ct);
 
   
}