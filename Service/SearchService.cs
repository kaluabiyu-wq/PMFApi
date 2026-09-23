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
        var userLocation = await context.Locations
            .AsNoTracking()
            .Where(l => l.Id == request.LocationId)
            .Select(l => new { l.Coordinate })
            .FirstOrDefaultAsync(ct);

        if (userLocation is null)
        {
            logger.LogWarning("Search create failed: LocationId {LocationId} not found (UserId {UserId})",
                request.LocationId, userId);
            throw new KeyNotFoundException($"Location {request.LocationId} was not found.");
        }

        var term = request.MedicineSearch.Trim();

        var matches = await context.Inventories
            .AsNoTracking()
            .Where(i => i.Medicine.IsActive
                        && i.Pharmacy.IsActive
                        && i.Pharmacy.IsVerified
                        && (EF.Functions.ILike(i.Medicine.GenericName, $"%{term}%")
                            || (i.Medicine.BrandName != null && EF.Functions.ILike(i.Medicine.BrandName, $"%{term}%"))))
            .Select(i => new
            {
                i.PharmacyId,
                PharmacyName = i.Pharmacy.Name,
                i.MedicineId,
                i.Medicine.GenericName,
                i.Medicine.BrandName,
                Status = i.Status.ToString(),
                i.Price,
                i.LastUpdatedAt,
                LocationLabel = i.Pharmacy.Location.Label,
                i.Pharmacy.Location.Coordinate.Latitude,
                i.Pharmacy.Location.Coordinate.Longitude
            })
            .ToListAsync(ct);

        var results = matches
            .Select(m => new SearchResultItem(
                m.PharmacyId,
                m.PharmacyName,
                m.MedicineId,
                m.GenericName,
                m.BrandName,
                m.Status,
                m.Price,
                CalculateDistanceKm(userLocation.Coordinate.Latitude, userLocation.Coordinate.Longitude, m.Latitude, m.Longitude),
                m.LocationLabel,
                m.LastUpdatedAt))
            .OrderBy(r => r.DistanceKm)
            .ToList();

        var search = new Search
        {
            UserId = userId,
            MedicineSearch = request.MedicineSearch,
            LocationId = request.LocationId,
            ResultCount = results.Count,
            SearchedAt = DateTime.UtcNow
        };

        context.Searches.Add(search);
        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Created Search {SearchId} for User {UserId} at Location {LocationId} - {ResultCount} results",
            search.Id, search.UserId, search.LocationId, results.Count);

        return new SearchResponse(
            search.Id, search.UserId, search.MedicineSearch, search.LocationId,
            search.ResultCount, search.SearchedAt, results);
    }

    public async Task<SearchResponse?> GetByIdAsync(int id, int userId, CancellationToken ct)
    {
        var log = await context.Searches
            .AsNoTracking()
            .Where(s => s.Id == id && s.UserId == userId)
            .Select(s => new { s.Id, s.UserId, s.MedicineSearch, s.LocationId, s.ResultCount, s.SearchedAt })
            .FirstOrDefaultAsync(ct);

        if (log is null) return null;

            return new SearchResponse(
            log.Id, log.UserId, log.MedicineSearch, log.LocationId,
            log.ResultCount, log.SearchedAt, Array.Empty<SearchResultItem>());
    }

    private static double CalculateDistanceKm(decimal lat1, decimal lon1, decimal lat2, decimal lon2)
    {
        const double earthRadiusKm = 6371.0;
        var dLat = ToRadians((double)(lat2 - lat1));
        var dLon = ToRadians((double)(lon2 - lon1));
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(ToRadians((double)lat1)) * Math.Cos(ToRadians((double)lat2))
                  * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadiusKm * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}