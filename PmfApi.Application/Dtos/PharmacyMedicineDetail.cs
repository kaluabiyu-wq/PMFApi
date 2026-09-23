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


    // one medicine with all pharmacies
    public sealed record MedicinePharmacyInventoryResponse(
    int MedicineId,
    string GenericName,
    string? BrandName,
    string? Category,
    string? DosageForm,
    string? Strength,
    bool RequiresPrescription,
    List<PharmacyInventoryDetail> Pharmacies
);

public sealed record PharmacyInventoryDetail(
    int PharmacyId,
    string Name,
    decimal Price,
    string Status,
    DateTime LastUpdatedAt
);