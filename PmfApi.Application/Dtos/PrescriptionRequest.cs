using System.ComponentModel.DataAnnotations;
using PmfApi.Domain.Entities;

namespace PmfApi.Application.Dtos;

public record PrescriptionRequest
{
    
    [Required, MaxLength(2048)]
    public required string FileUrl { get; init; }
}


public record PrescriptionReviewRequest
{
   
    [Required]
    public required VerificationStatus VerificationStatus { get; init; }

    
    [MaxLength(500)]
    public string? Note { get; init; }
}