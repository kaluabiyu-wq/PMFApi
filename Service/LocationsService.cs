

using Microsoft.EntityFrameworkCore;
using PmfApi.Data;
using PmfApi.Dto;
using PmfApi.Entities;
using PmfApi.Interface;

namespace PmfApi.Service;
public class LocationService(PmfDbContext context, ILogger<LocationService> logger) :
ILocationService
{
     public Task<LocationResponse?> GetByCoordinateAsync(Coordinate coordinate,CancellationToken ct) =>
     context.Locations.AsNoTracking()
     .Where(l => l.Coordinate == coordinate)
     .Select(l => new LocationResponse (
     l.Id,l.Label,l.Subcity , l.Woreda, l.Coordinate
     )).FirstOrDefaultAsync(ct);

     public async Task<LocationResponse> CreateAsync (LocationRequest request,CancellationToken ct)
    {
        var location = new Location
        {
            Label = request.Label,
            Subcity = request.Subcity,
            Coordinate = request.Coordinate,
            Woreda = request.Woreda
        };

        context.Locations.Add(location);
        await context.SaveChangesAsync(ct);
        logger.LogInformation(" Location {LocationId} {Label} in {Subcity} subcity ",
        location.Id,location.Label,location.Subcity);


        return (await GetByCoordinateAsync(location.Coordinate,ct))!;
    }


     
    
}