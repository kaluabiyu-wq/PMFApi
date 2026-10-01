using PmfApi.Domain.Entities;

namespace PmfApi.Application.Dtos;

public record OrderListQuery : PagedRequest
{
    public OrderStatus? Status {get;set;}
}