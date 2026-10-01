using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PmfApi.Application.Dtos;
using PmfApi.Application.Interfaces;
using PmfApi.Domain.Entities;

namespace PmfApi.Infrastructure.Persistence.Services;

public class OrderItemService(PmfDbContext context, ILogger<OrderItemService> logger) : IOrderItemService
{
    public async Task<ServiceResult<OrderResponse>> AddAsync(int orderId, OrderItemRequest request, CancellationToken ct)
    {
        await using var tx = await context.Database.BeginTransactionAsync(ct);

        var blocked = await LockPendingOrderAsync(orderId, ct);
        if (blocked is not null) return blocked;

        var orderPharmacyId = await context.Orders
            .Where(o => o.Id == orderId).Select(o => o.PharmacyId).FirstAsync(ct);

        var inventory = await context.Inventories.AsNoTracking()
            .Where(i => i.Id == request.InventoryId)
            .Select(i => new { i.PharmacyId, i.Price, MedicineIsActive = i.Medicine.IsActive })
            .FirstOrDefaultAsync(ct);

        if (inventory is null)
            return ServiceResult<OrderResponse>.Invalid($"Inventory item {request.InventoryId} does not exist.");

        if (inventory.PharmacyId != orderPharmacyId)
            return ServiceResult<OrderResponse>.Invalid("That item belongs to a different pharmacy than the rest of this order.");

        if (!inventory.MedicineIsActive)
            return ServiceResult<OrderResponse>.Invalid("That medicine is no longer available.");

        if (await context.OrderItems.AnyAsync(i => i.OrderId == orderId && i.InventoryId == request.InventoryId, ct))
            return ServiceResult<OrderResponse>.Conflict("That item is already in the order; update its quantity instead.");

        context.OrderItems.Add(new OrderItem
        {
            OrderId = orderId,
            InventoryId = request.InventoryId,
            Quantity = request.Quantity,
            UnitPrice = inventory.Price,
        });

        try
        {
            await context.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex) when (PostgresErrors.IsForeignKeyViolation(ex))
        {
            return ServiceResult<OrderResponse>.Conflict("The item was removed while it was being added. Reload and try again.");
        }

        logger.LogInformation("Inventory {InventoryId} x{Quantity} added to Order {OrderId}", request.InventoryId, request.Quantity, orderId);
        return await ReloadAsync(orderId, ct);
    }

    public async Task<ServiceResult<OrderResponse>> UpdateQuantityAsync(int orderId, int itemId, int quantity, CancellationToken ct)
    {
        await using var tx = await context.Database.BeginTransactionAsync(ct);

        var blocked = await LockPendingOrderAsync(orderId, ct);
        if (blocked is not null) return blocked;

        var item = await context.OrderItems.FirstOrDefaultAsync(i => i.Id == itemId && i.OrderId == orderId, ct);
        if (item is null)
            return ServiceResult<OrderResponse>.NotFound($"Order {orderId} has no item {itemId}.");

        // UnitPrice is NOT refreshed: the patient keeps the price they saw when the line was added.
        item.Quantity = quantity;
        await context.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        logger.LogInformation("Order {OrderId} item {ItemId} quantity set to {Quantity}", orderId, itemId, quantity);
        return await ReloadAsync(orderId, ct);
    }

    public async Task<ServiceResult<OrderResponse>> RemoveAsync(int orderId, int itemId, CancellationToken ct)
    {
        await using var tx = await context.Database.BeginTransactionAsync(ct);

        var blocked = await LockPendingOrderAsync(orderId, ct);
        if (blocked is not null) return blocked;

        if (!await context.OrderItems.AnyAsync(i => i.Id == itemId && i.OrderId == orderId, ct))
            return ServiceResult<OrderResponse>.NotFound($"Order {orderId} has no item {itemId}.");

        if (await context.OrderItems.CountAsync(i => i.OrderId == orderId, ct) == 1)
            return ServiceResult<OrderResponse>.Invalid("An order must keep at least one item. Cancel the order instead.");

        await context.OrderItems.Where(i => i.Id == itemId && i.OrderId == orderId).ExecuteDeleteAsync(ct);
        await tx.CommitAsync(ct);

        logger.LogInformation("Item {ItemId} removed from Order {OrderId}", itemId, orderId);
        return await ReloadAsync(orderId, ct);
    }

   
    private async Task<ServiceResult<OrderResponse>?> LockPendingOrderAsync(int orderId, CancellationToken ct)
    {
        var locked = await context.Orders
            .Where(o => o.Id == orderId && o.Status == OrderStatus.Pending)
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.UpdatedAt, DateTime.UtcNow), ct);

        if (locked == 1) return null;

        return await context.Orders.AnyAsync(o => o.Id == orderId, ct)
            ? ServiceResult<OrderResponse>.Conflict("Items can only be changed while the order is Pending.")
            : ServiceResult<OrderResponse>.NotFound($"No order exists with id {orderId}.");
    }

    private async Task<ServiceResult<OrderResponse>> ReloadAsync(int orderId, CancellationToken ct) =>
        ServiceResult<OrderResponse>.Ok(await context.Orders.AsNoTracking()
            .Where(o => o.Id == orderId)
            .Select(OrderProjections.ToResponse)
            .FirstAsync(ct));
}