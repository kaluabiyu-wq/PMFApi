
using System.ComponentModel.DataAnnotations;
using PmfApi.Domain.Entities;

namespace PmfApi.Application.Dtos;

public record OrderRequest
{
    [Required, MinLength(1), MaxLength(50)]
    public required List<OrderItemRequest> Items {get;init;}

}

public record OrderItemRequest
{
    [Range(1, int.MaxValue)]
    public required int InventoryId {get;init;}

    [Range(1,100)]
    public int Quantity {get;init;} = 1;
}

public record OrderItemQuantityRequest
{
    [Range(1, 100)]
    public required int Quantity {get;init;}
}