using PmfApi.Domain.Entities;

namespace PmfApi.Application.Dtos;


public record PrescriptionListQuery : PagedRequest
{
    public VerificationStatus? Status { get; init; }
}