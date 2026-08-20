

using Microsoft.EntityFrameworkCore;
using PmfApi.Data;
using PmfApi.Dto;
using PmfApi.Entities;
using PmfApi.Interface;

namespace PmfApi.Service;
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
    public async Task<LocationResponse> CreateAsync(LocationRequest request, CancellationToken ct)
{
    var existing = await GetByCoordinateAsync(0, request.Coordinate, ct); // or whatever id makes sense here

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
    await context.SaveChangesAsync(ct);

    return new LocationResponse(location.Id, location.Label, location.Subcity, location.Woreda, location.Coordinate);
}
    
}