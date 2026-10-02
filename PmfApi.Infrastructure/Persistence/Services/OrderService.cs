

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PmfApi.Application.Dtos;
using PmfApi.Application.Interfaces;
using PmfApi.Domain.Entities;

namespace PmfApi.Infrastructure.Persistence.Services;

public class OrderService(PmfDbContext context, ILogger<OrderService> logger) :
 IOrderService
{
    public async Task<ServiceResult<OrderResponse>> CreateAsync(int userId, OrderRequest request,
    CancellationToken ct)
    {
        var lines = request.Items;

        if(lines.GroupBy(i => i.InventoryId).Any(g=>g.Count() > 1))
          return ServiceResult<OrderResponse>.Invalid("Each inventory item may appear only once: combine the quantities instead.");

        var inventoryIds = lines.Select(i => i.InventoryId).ToList();

        var inventories = await context.Inventories.AsNoTracking()
        .Where(i=> inventoryIds.Contains(i.Id))
        .Select(i => new
        {
            i.Id,
            i.PharmacyId,
            i.Price,
            PharmacyIsActive = i.Pharmacy.IsActive,
            MedicineIsActive = i.Medicine.IsActive
        }
        ).ToListAsync(ct);

        if(inventories.Count != inventoryIds.Count)
         return ServiceResult<OrderResponse>.Invalid("One Or more inventory items do not exist.");
       
        if(inventories.Select(i => i.PharmacyId).Distinct().Count() != 1)
        return ServiceResult<OrderResponse>.Invalid("All items in one Order must come from the same pharmacy. Place a separate order per pharmacy.");

        if(inventories.Any(i=>!i.PharmacyIsActive))
        return ServiceResult<OrderResponse>.Invalid("This pharmacy is not accepting orders.");

        if(inventories.Any(i=>!i.MedicineIsActive))
        return ServiceResult<OrderResponse>.Invalid("One or more medicine are no longer available");

        var now = DateTime.UtcNow;
        var priceById = inventories.ToDictionary(i=>i.Id, i=>i.Price);

        var order = new Order
        {
            UserId = userId,
            PharmacyId = inventories[0].PharmacyId,
            Status = OrderStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now,
            Items = lines.Select(l => new OrderItem
            {
                InventoryId = l.InventoryId,
                Quantity = l.Quantity,
                UnitPrice = priceById[l.InventoryId],
            }).ToList(),
            
        };

        context.Orders.Add(order);

     try
        {
            
            await context.SaveChangesAsync(ct);
        }
        catch (Exception ex) when (PostgresErrors.IsForeignKeyViolation(ex))
        {
            return ServiceResult<OrderResponse>.Conflict("An item or your account was removed while the order was being placed. Reload and try again.");
        }

        logger.LogInformation("User {UserId} placed Order {OrderId} with Pharmacy {PharmacyId} ({ItemCount} items)",
            order.UserId, order.Id, order.PharmacyId, order.Items.Count);

        return ServiceResult<OrderResponse>.Ok((await GetByIdAsync(order.Id, ct))!);
    }

    public Task<OrderResponse?> GetByIdAsync(int id, CancellationToken ct) =>
        context.Orders.AsNoTracking()
            .Where(o => o.Id == id)
            .Select(OrderProjections.ToResponse)
            .FirstOrDefaultAsync(ct);

    public Task<PagedResponse<OrderResponse>> GetByUserAsync(int userId, OrderListQuery query, CancellationToken ct) =>
        PageAsync(context.Orders.AsNoTracking().Where(o => o.UserId == userId), query, ct);

    public Task<PagedResponse<OrderResponse>> GetByPharmacyAsync(int pharmacyId, OrderListQuery query, CancellationToken ct) =>
        PageAsync(context.Orders.AsNoTracking().Where(o => o.PharmacyId == pharmacyId), query, ct);

    public async Task<ServiceResult<OrderResponse>> ChangeStatusAsync(int id, OrderStatus newStatus, bool byPharmacy, CancellationToken ct)
    {
        var row = await context.Orders.AsNoTracking()
            .Where(o => o.Id == id)
            .Select(o => new
            {
                o.Status,
                RequiresRx = o.Items.Any(i => i.Inventory.Medicine.RequeiresPrescription),
                HasApprovedPrescription = o.Prescriptions.Any(p => p.VerificationStatus == VerificationStatus.Approved),
            })
            .FirstOrDefaultAsync(ct);

        if (row is null)
            return ServiceResult<OrderResponse>.NotFound($"No order exists with id {id}.");

        var current = row.Status;

        if (!Order.CanTransition(current, newStatus, byPharmacy))
            return ServiceResult<OrderResponse>.Conflict($"An order that is {current} cannot be moved to {newStatus} by this caller.");

        if (newStatus == OrderStatus.Confirmed && row.RequiresRx && !row.HasApprovedPrescription)
            return ServiceResult<OrderResponse>.Conflict(
                "This order contains prescription-only medicine. It can be confirmed only after its prescription has been approved.");

        
        var target = context.Orders.Where(o => o.Id == id && o.Status == current);

        if (newStatus == OrderStatus.Confirmed)
        {
            target = target.Where(o =>
                !o.Items.Any(i => i.Inventory.Medicine.RequeiresPrescription)
                || o.Prescriptions.Any(p => p.VerificationStatus == VerificationStatus.Approved));
        }

        var rows = await target
            .ExecuteUpdateAsync(s => s
                .SetProperty(o => o.Status, newStatus)
                .SetProperty(o => o.UpdatedAt, DateTime.UtcNow), ct);

        if (rows == 0)
            return ServiceResult<OrderResponse>.Conflict("The order was changed by someone else a moment ago. Reload it and try again.");

        logger.LogInformation("Order {OrderId} moved {From} -> {To} (byPharmacy: {ByPharmacy})", id, current, newStatus, byPharmacy);

        return ServiceResult<OrderResponse>.Ok((await GetByIdAsync(id, ct))!);
    }

    private static async Task<PagedResponse<OrderResponse>> PageAsync(IQueryable<Order> source, OrderListQuery query, CancellationToken ct)
    {
        if (query.Status is { } status)
            source = source.Where(o => o.Status == status);

        var page = Math.Max(1, query.Page);

        var total = await source.CountAsync(ct);

        var items = await source
            .OrderByDescending(o => o.CreatedAt).ThenByDescending(o => o.Id)
            .Skip((page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(OrderProjections.ToResponse)
            .ToListAsync(ct);

        return new PagedResponse<OrderResponse>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = query.PageSize,
        };
    }
}