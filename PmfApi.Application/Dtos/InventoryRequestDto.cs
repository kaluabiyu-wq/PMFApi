
using System.ComponentModel.DataAnnotations;

namespace PmfApi.Application.Dtos;

public record InventoryRequest
{
    public required int MedicineId {get;init;}

    public int UpdatebyUserId {get;init;}

    [Range(1, 1_000_000, ErrorMessage = "Price must be greater than one.")]
    public decimal Price {get;init;}

    public string  Status {get;init;} = "Fresh";

}