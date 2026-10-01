using System.Linq.Expressions;
using PmfApi.Application.Dtos;
using PmfApi.Domain.Entities;

namespace PmfApi.Infrastructure.Persistence.Services;

internal static class OrderProjections
{
    public static readonly Expression<Func<Order, OrderResponse>> ToResponse = o 
    => new OrderResponse(
        o.Id,
        o.UserId,
        o.PharmacyId,
        o.Status,
        o.CreatedAt,
        o.UpdatedAt,
        o.Items.Sum(i => i.Quantity * i.UnitPrice),
        o.Items.OrderBy(i => i.Id).Select(i => new OrderItemResponse(
            i.Id,
            i.OrderId,
            i.InventoryId,
            i.Inventory.MedicineId,
            i.Inventory.Medicine.GenericName,
            i.Inventory.Medicine.BrandName,
            i.Quantity,
            i.UnitPrice,
            i.Quantity * i.UnitPrice
        )).ToList());
}