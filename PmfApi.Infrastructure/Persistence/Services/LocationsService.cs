

using Microsoft.EntityFrameworkCore;
using PmfApi.Infrastructure.Persistence;
using PmfApi.Application.Dtos;
using PmfApi.Domain.Entities;
using PmfApi.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace PmfApi.Infrastructure.Persistence.Services;
public class LocationService(PmfDbContext context, ILogger<LocationService> logger) :
ILocationService
{
    public Task<LocationResponse?> GetByCoordinateAsync(int id, Coordinate coordinate, CancellationToken ct) =>
    context.Locations.AsNoTracking()
        .Where(l => l.Id == id 
            && l.Coordinate.Latitude == coordinate.Latitude 
            && l.Coordinate.Longitude == coordinate.Longitude)
        .Select(l => new LocationResponse(
            l.Id, l.Label, l.Subcity, l.Woreda, l.Coordinate
        ))
        .FirstOrDefaultAsync(ct);

  private Task<LocationResponse?> FindByCoordinateAsync(Coordinate coordinate, CancellationToken ct) =>
        context.Locations.AsNoTracking()
            .Where(l => l.Coordinate.Latitude == coordinate.Latitude
                     && l.Coordinate.Longitude == coordinate.Longitude)
            .Select(l => new LocationResponse(
                l.Id, l.Label, l.Subcity, l.Woreda, l.Coordinate))
            .FirstOrDefaultAsync(ct);
    public async Task<LocationResponse> CreateAsync(LocationRequest request, CancellationToken ct)
{
    var existing = await FindByCoordinateAsync(request.Coordinate, ct);

        if (existing is not null)
            return existing;

    var location = new Location
    {
      
        Label = request.Label,
        Subcity = request.Subcity,
        Woreda = request.Woreda,
        Coordinate = request.Coordinate
    };

    context.Locations.Add(location);
    try
        {
            await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Another request inserted the same coordinate between our lookup and insert.
            context.Entry(location).State = EntityState.Detached;

            var winner = await FindByCoordinateAsync(request.Coordinate, ct);
            if (winner is not null)
            {
                logger.LogInformation(
                    "Location insert raced at ({Lat}, {Lng}); returning existing {LocationId}.",
                    request.Coordinate.Latitude, request.Coordinate.Longitude, winner.Id);
                return winner;
            }

            throw;
        }

        return new LocationResponse(location.Id, location.Label, location.Subcity, location.Woreda, location.Coordinate);
    }

public async  Task<PagedResponse<LocationResponse>> GetLocationAsync(PagedRequest request, CancellationToken ct)
{
    IQueryable<Location> query = context.Locations.AsNoTracking();

    if (!string.IsNullOrWhiteSpace(request.Search))
    {
               query = query.Where(c => EF.Functions.ILike(c.Label, $"%{request.Search}%")
                               || EF.Functions.ILike(c.Subcity, $"%{request.Search}%"));
    }

    var totalCount = await query.CountAsync(ct);

    IOrderedQueryable<Location> sortedQuery = request.OrderBy switch
    {
        "Label" => request.Descending
            ? query.OrderByDescending(c => c.Label)
            : query.OrderBy(c => c.Label),
        "Subcity" => request.Descending
            ? query.OrderByDescending(c => c.Subcity)
            : query.OrderBy(c => c.Subcity),
        "Coordinate" => request.Descending
            ? query.OrderByDescending(c => c.Coordinate)
            : query.OrderBy(c => c.Coordinate),
        _ =>  request.Descending
            ? query.OrderByDescending(c => c.Woreda)
            : query.OrderBy(c => c.Woreda)
    };

    var items = await sortedQuery
        .Skip((request.Page - 1) * request.PageSize)
        .Take(request.PageSize)
        .Select(l => new LocationResponse(l.Id,
        l.Label,l.Subcity,l.Woreda,l.Coordinate))
        .ToListAsync(ct);

    return new PagedResponse<LocationResponse>
    {
        Items = items,
        TotalCount = totalCount,
        Page = request.Page,
        PageSize = request.PageSize
    };
}
}