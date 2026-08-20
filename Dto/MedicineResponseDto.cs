

namespace PmfApi.Dto;

public record MedicineResponse
(
    int Id,
    string GenericName,
    string BrandName,
    string? Category,
    string? DosageForm,
    string? Strength,
    bool RequeiresPrescription,
    bool IsActive
);