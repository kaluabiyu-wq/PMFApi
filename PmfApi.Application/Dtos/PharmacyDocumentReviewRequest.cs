using System.ComponentModel.DataAnnotations;
using PmfApi.Domain.Entities;

namespace PmfApi.Application.Dtos;

public record PharmacyDocumentReviewRequest
{
    public required int ReviewedByUserId {get;init;}

    public required ReviewStatus ReviewStatus {get;init;}

    public string? Reason {get;init;}
}