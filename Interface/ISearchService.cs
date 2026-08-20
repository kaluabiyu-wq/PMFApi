using PmfApi.Dto;
using PmfApi.Entities;


namespace PmfApi.Interface;

public interface ISearchService
{
Task<SearchResponse> CreateAsync(int userId,SearchRequest request,CancellationToken ct);
Task<SearchResponse?> GetByIdAsync(int id, int UserId, CancellationToken ct);

}