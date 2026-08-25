namespace PmfApi.Application.Dtos;


public sealed record PharmacyMedicineDetail(
    int MedicineId,
    string GenericName,
    string BrandName,
    string? Category,
    string? DosageForm,
    string? Strength,
    bool RequiresPrescription,
    decimal Price,
    string Status,
    DateTime LastUpdatedAt);