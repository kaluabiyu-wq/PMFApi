

using System.Text.Json.Serialization;
using PmfApi.Domain.Entities;

namespace PmfApi.Application.Dtos;

public record OrderItemResponse(
   int Id,
   int OrderId,
   int InventoryId,
   int MedicineId,
   string GenericName,
   string BrandName,
   int Quantity,
   decimal UnitPrice,
   decimal LineTotal

);

public record OrderResponse(
    int Id,
    int UserId,
    int PharmacyId,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] OrderStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    decimal TotalPrice,
    IReadOnlyList<OrderItemResponse> Items
);