using Microsoft.EntityFrameworkCore;
using PmfApi.Data;
using PmfApi.Dto;
using PmfApi.Entities;
using PmfApi.Interface;

namespace PmfApi.Service;

public class SearchService(PmfDbContext context, ILogger<SearchService> logger)
    : ISearchService
{
    public async Task<SearchResponse> CreateAsync(int userId, SearchRequest request, CancellationToken ct)
    {
        var locationExists = await context.Locations
            .AnyAsync(l => l.Id == request.LocationId, ct);

        if (!locationExists)
        {
            logger.LogWarning("Search create failed: LocationId {LocationId} not found (UserId {UserId})",
                request.LocationId, userId);
            throw new KeyNotFoundException($"Location {request.LocationId} was not found.");
        }

        var search = new Search
        {
            UserId = userId,
            MedicineSearch = request.MedicineSearch,
            LocationId = request.LocationId,
            ResultCount = request.ResultCount,
            SearchedAt = DateTime.UtcNow
        };

        context.Searches.Add(search);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("Created Search {SearchId} for User {UserId} at Location {LocationId}",
            search.Id, search.UserId, search.LocationId);

        return (await GetByIdAsync(search.Id, search.UserId, ct))!;
    }

    public Task<SearchResponse?> GetByIdAsync(int id, int userId, CancellationToken ct) =>
        context.Searches.AsNoTracking()
            .Where(s => s.Id == id && s.UserId == userId)
            .Select(s => new SearchResponse(
                s.Id, s.UserId, s.MedicineSearch, s.LocationId, s.ResultCount, s.SearchedAt
            )).FirstOrDefaultAsync(ct);
}