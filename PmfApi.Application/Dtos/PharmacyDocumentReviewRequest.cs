using PmfApi.Domain.Entities;

namespace PmfApi.Application.Dtos;

public record PharmacyDocumentReviewRequest
{
    public required int ReviewedByUserId {get;init;}

    public required ReviewStatus ReviewStatus {get;init;}
}
