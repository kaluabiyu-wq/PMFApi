using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace PmfApi.Application.Dtos;


public class PrescriptionUploadRequest
{
    [Required]
    public IFormFile File { get; set; } = null!;
}
