namespace PmfApi.Application.Dtos;

public record ReviewResponse(
    int Id,
    int UserId,
    int PharmacyId,
    int Rating,
    string? Comment,
    DateTime SubmittedAt
);
