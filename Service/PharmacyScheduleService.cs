using System.ComponentModel.DataAnnotations.Schema;



public class PharmaciesScheduleService : IPharmaciesScheduleService
{
    private readonly Dictionary<string, PharmaciesScheduleRecord> _store = new();
    private readonly ILogger<PharmaciesScheduleService> _logger;
   public PharmaciesScheduleService(ILogger<PharmaciesScheduleService> logger)
    {
       _logger = logger;
    }

    public Task<PharmaciesScheduleRecord> CreateAsync( 
    string  pharmacyId ,
    int dayOfWeek,
    DateTime openTime,
    DateTime closedTime,
    bool isClosed)
    {
        var existing = _store.Values
        .FirstOrDefault(p =>  p.PharmacyId == pharmacyId);
        if(existing is not null )
        {
            _logger.LogWarning(
            "Duplicate Pharmacies {PharmacyId} schedule already exists (record {PharmaciesScheduleId})",
            pharmacyId,existing.Id);
               return Task.FromResult(existing);
        }
          var id = Guid.NewGuid().ToString("N")[..8];

          var pharmaciesSchedule = new PharmaciesScheduleRecord(id,pharmacyId , dayOfWeek,
         openTime, closedTime, isClosed);
       _store[id] = pharmaciesSchedule;
       _logger.LogInformation(
 "Created Pharmacies Schedule {PharmacyId} {DayOfWeek} {OpenTime} {ClosedTime} {IsClosed} is record in *({PharmacySchduleId})",
 id,pharmacyId , dayOfWeek,
         openTime, closedTime, isClosed
       );
       return Task.FromResult(pharmaciesSchedule);
    }
     
     public Task<PharmaciesScheduleRecord?> GetByIdAsync(string id)
    {
        _store.TryGetValue(id,out var pharmaciesSchedule);

        if(pharmaciesSchedule is null)
        {
            _logger.LogWarning("Pharmacies {PharmacySchduleId} not found",id);
        }
        return Task.FromResult(pharmaciesSchedule);
    }

    public Task<IReadOnlyList<PharmaciesScheduleRecord>> GetAllAsync()
    {
        IReadOnlyList<PharmaciesScheduleRecord> all = _store.Values.ToList();

        return Task.FromResult(all);
    }
    public Task<bool> DeleteAsync(string id)
    {
        var removed = _store.Remove(id);
        if(removed)
        {
            _logger.LogWarning("Deleted Pharmacies {PharmacySchduleId} schedule",id);
        }
        else
        {
            _logger.LogWarning("Deleted faild. Pharmacies Schedule {PharmacySchduleId}",id);
        }
        return Task.FromResult(removed);
    }


}

public record PharmaciesScheduleRecord(
    string Id,
    string  PharmacyId ,
    int DayOfWeek,
    DateTime OpenTime,
    DateTime ClosedTime,
    bool IsClosed
);