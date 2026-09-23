

namespace PmfApi.Dto;

public record SearchResponse
(
    int Id,
    int UserId,
    string MedicineSearch,
    int LocationId,
    int ResultCount,
    DateTime SearchedAt

);