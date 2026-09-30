using PmfApi.Application.Dtos;

namespace PmfApi.Application.Interfaces;

public interface IPharmacyRegistrationService
{
    Task<RegisterPharmacyResponse> RegisterAsync(RegisterPharmacyRequest request, CancellationToken ct);
}