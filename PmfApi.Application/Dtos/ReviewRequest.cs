using System.ComponentModel.DataAnnotations;

namespace PmfApi.Application.Dtos;

public record ReviewRequest
{
    public required int UserId {get;init;}

    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
    public required int Rating {get;init;}

    [MaxLength(2000)]
    public string? Comment {get;init;}
}
