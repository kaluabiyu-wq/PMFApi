using System.ComponentModel.DataAnnotations;
using PmfApi.Domain.Entities;

namespace PmfApi.Application.Dtos;

public record PharmacyDocumentRequest
{
    public required DocumentType DocumentType {get;init;}

    [Required]
    public required string FileUrl {get;init;}

    public DateTime? ExpiresAt {get;init;}
}
