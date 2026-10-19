using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using PmfApi.Domain.Entities;

namespace PmfApi.Application.Dtos;

/// <summary>multipart/form-data body for adding a new verification document.</summary>
public class PharmacyDocumentUploadRequest
{
    [Required]
    public DocumentType DocumentType { get; set; }

    [Required]
    public IFormFile File { get; set; } = null!;

    public DateTime? ExpiresAt { get; set; }
}


public class PharmacyDocumentReplaceRequest
{
    [Required]
    public IFormFile File { get; set; } = null!;

    public DateTime? ExpiresAt { get; set; }
}