

using PmfApi.Application.Dtos;

namespace PmfApi.Application.Interfaces;

public interface IOrderItemService
{
    Task<ServiceResult<OrderResponse>> AddAsync(int orderId, OrderItemRequest request, CancellationToken ct);
    Task<ServiceResult<OrderResponse>> UpdateQuantityAsync(int orderId, int itemId, int quantity, CancellationToken ct);

    Task<ServiceResult<OrderResponse>> RemoveAsync(int orderId,int itemId, CancellationToken ct);


}