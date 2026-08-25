
using PmfApi.Application.Dtos;

namespace PmfApi.Application.Interfaces;

public interface ISearchService
{
    Task<SearchResponse> CreateAsync(int userId, SearchRequest request, CancellationToken ct);
    Task<SearchResponse?> GetByIdAsync(int id, int userId, CancellationToken ct);
}