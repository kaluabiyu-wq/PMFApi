
using System.ComponentModel.DataAnnotations;

namespace PmfApi.Application.Dtos;

public record InventoryUpdateRequest
{
    [Range(1,1_000_000, ErrorMessage = "Price must be at least 1.")]
    public decimal? Price {get;init;}
    [MinLength(1), MaxLength(20)]
    public string? Status {get;init;}
}