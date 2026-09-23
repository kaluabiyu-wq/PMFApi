

using Microsoft.EntityFrameworkCore;
using PmfApi.Data;
using PmfApi.Dto;
using PmfApi.Entities;
using PmfApi.Interface;

namespace PmfApi.Service;
public class LocationService(PmfDbContext context, ILogger<LocationService> logger) :
ILocationService
{
     public Task<LocationResponse?> GetByLongitudAsync(decimal longitude, decimal latitude,CancellationToken ct) =>
     context.Locations.AsNoTracking()
     .Where(l => l.Latitude == latitude && l.Longitude == longitude)
     .Select(l => new LocationResponse (
     l.Id,l.Label,l.Subcity , l.Woreda, l.Longitude,l.Latitude
     )).FirstOrDefaultAsync(ct);

     public async Task<LocationResponse> CreateAsync (LocationRequest request,CancellationToken ct)
    {
        var location = new Location
        {
            Label = request.Label,
            Subcity = request.Subcity,
            Longitude = request.Longitude,
            Latitude = request.Latitiude,
            Woreda = request.Woreda
        };

        context.Locations.Add(location);
        await context.SaveChangesAsync(ct);
        logger.LogInformation(" Location {LocationId} {Label} in {Subcity} subcity with {Longitude} Longitude and{Latitude} Latiude",
        location.Id,location.Label,location.Subcity,location.Longitude,location.Latitude);


        return (await GetByLongitudAsync(location.Longitude,location.Latitude,ct))!;
    }


     
    
}