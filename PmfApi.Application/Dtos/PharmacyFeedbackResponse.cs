namespace PmfApi.Application.Dtos;

public sealed record PharmacyFeedbackResponse(
    int Id,
    int InventoryId,
    string MedicineName,
    bool WasMedicineAvailable,
    string? Comments,
    DateTime SubmittedAt);