

namespace PmfApi.Application.Dtos;


public record SearchResultItem(
    int PharmacyId,
    string PharmacyName,
    int MedicineId,
    string GenericName,
    string? BrandName,
    string Status,
    decimal Price,
    double DistanceKm,
    string LocationLabel,
    DateTime LastUpdatedAt);

public record SearchResponse(
    int Id,
    int UserId,
    string MedicineSearch,
    int LocationId,
    int ResultCount,
    DateTime SearchedAt,
    IReadOnlyList<SearchResultItem> Results);