public interface ILocationService
{
Task<LocationRecord> CreateAsync(string label,decimal latitude,
decimal longitude,string subcity,
string woreda,string city);
Task<LocationRecord?> GetByIdAsync(string id);

Task<IReadOnlyList<LocationRecord>> GetAllAsync();

Task<bool> DeleteAsync(string id);


}
public class LocationService : ILocationService
{
  private readonly Dictionary<string, LocationRecord> _store = new ();

  private readonly ILogger<LocationService> _logger;

  public LocationService(ILogger<LocationService> logger)
    {
        _logger = logger;
    }

    public Task<LocationRecord> CreateAsync(string label,decimal latitude,
decimal longitude,string subcity,
string woreda,string city)
    {
     var existing = _store.Values
     .FirstOrDefault(
            l => l.Latitude == latitude && l.Longitude == longitude);
     if(existing is not null)
        {
            _logger.LogWarning(
                "Duplicate Location {Longitude} {Latitude} already exitst in {LocationId}",
                latitude,longitude,existing.Id);
            return Task.FromResult(existing);
           
        }

        var id = Guid.NewGuid().ToString("N")[..8];
        var location = new LocationRecord(id,label,latitude,longitude,subcity,woreda,city);
        _store[id] = location ;

        _logger.LogInformation(
            "Created Location {Label}  {Latitude} {Longitude} {Subcity} {Woreda} {city} record {LoctionId}",label,
            latitude,longitude,subcity,woreda,city,id);

         return Task.FromResult(location);
    }

    public Task<LocationRecord?> GetByIdAsync(string id)
    {
        _store.TryGetValue(id,out var location);
        if (location is null)
        {
            _logger.LogWarning(
                "Location {LocationId} not found",id
            );
        }
        return Task.FromResult(location);
    }



    public Task<IReadOnlyList<LocationRecord>> GetAllAsync()
    {
        IReadOnlyList<LocationRecord> all = _store.Values.ToList();
        return Task.FromResult(all);
    }

    public Task<bool> DeleteAsync(string id)
    {
        var removed = _store.Remove(id);
        if(removed)
        {
            _logger.LogInformation("Deleted Location {LocationId}",id);
        }
        else
        {
            _logger.LogWarning(
                "Deleted failed. Location {LocationId} not Found",id);
        }
        return Task.FromResult(removed);
    }

}

public record LocationRecord(
    string Id,
    string Label,
    decimal Latitude,
    decimal Longitude,
    string Subcity,
    string Woreda,
    string city
);