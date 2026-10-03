namespace PmfApi.Application.Dtos;
public record AlertListQuery : PagedRequest
{
    public bool? IsRead { get; init; }
}