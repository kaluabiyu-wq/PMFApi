namespace PmfApi.Application.Dtos;


public record PharmacyAdminQuery : PagedRequest
{
    public bool? IsVerified {get;init;}
    public bool? IsActive {get;init;}
    public string? Freshness {get;init;}
}