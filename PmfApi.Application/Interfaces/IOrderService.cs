
using PmfApi.Application.Dtos;
using PmfApi.Domain.Entities;

namespace PmfApi.Application.Interfaces;

public interface IOrderService
{
    Task<ServiceResult<OrderResponse>> CreateAsync(int userId, OrderRequest request, CancellationToken ct);
    Task<OrderResponse?> GetByIdAsync(int id, CancellationToken ct);
    Task<PagedResponse<OrderResponse>> GetByUserAsync(int UserId, OrderListQuery query, CancellationToken ct);
    Task<PagedResponse<OrderResponse>> GetByPharmacyAsync(int pharmacyId, OrderListQuery query, CancellationToken ct);
    Task<ServiceResult<OrderResponse>> ChangeStatusAsync(int id, OrderStatus newStatus, bool byPharmacy,CancellationToken ct);
    

}