namespace PmfApi.Application.Dtos;

public record FavoriteResponse(
    int Id,
    int UserId,
    int PharmacyId,
    DateTime CreatedAt
);
