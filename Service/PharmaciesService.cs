


public class PharamaciesSerivce : IPharmaciesService
{
    private readonly Dictionary<string, PharmaciesRecord> _store = new();
    private readonly ILogger<PharamaciesSerivce> _logger;
   public PharamaciesSerivce(ILogger<PharamaciesSerivce> logger)
    {
       _logger = logger;
    }

    public Task<PharmaciesRecord> CreateAsync(string name,string licenceNumber,int phoneNumber
   ,string email,bool isVerified,bool isActive,decimal relialbilityScore, int freshnessThreshold,
   DateTime lastinventoryUpdateAt,
    DateTime registeredAt)
    {
        var existing = _store.Values
        .FirstOrDefault(p => p.Name == name && p.LicenceNumber == licenceNumber);
        if(existing is not null )
        {
            _logger.LogWarning(
            "Duplicate Pharmacies {Name} {LicenceNumber} already exists (record {PharmaciesId})",
              name,licenceNumber,existing.Id);
               return Task.FromResult(existing);
        }
          var id = Guid.NewGuid().ToString("N")[..8];

          var pharmacies = new PharmaciesRecord(id,name,licenceNumber,phoneNumber
   ,email,isVerified,isActive,relialbilityScore,freshnessThreshold,
   DateTime.UtcNow,
    DateTime.UtcNow);
       _store[id] = pharmacies;
       _logger.LogInformation(
        "Created Pharmacies {Name} Licenese Number {LicenceNumber} PhoneNumber{PhoneNumber} Email {Email} IsVerified {IsVerified} IsActive {IsActive} ReliablityScore {RelialbilityScore} FreshnessThreshold {FreshnessThreshold} record {PharmaciesId}",
        name,licenceNumber,phoneNumber
   ,email,isVerified,isActive,relialbilityScore,freshnessThreshold,id
       );
       return Task.FromResult(pharmacies);
    }
     
     public Task<PharmaciesRecord?> GetByIdAsync (string id)
    {
        _store.TryGetValue(id,out var pharmacies);

        if(pharmacies is null)
        {
            _logger.LogWarning("Pharmacies {PharmaciesId} not found",id);
        }
        return Task.FromResult(pharmacies);
    }

    public Task<IReadOnlyList<PharmaciesRecord>> GetAllAsync()
    {
        IReadOnlyList<PharmaciesRecord> all = _store.Values.ToList();

        return Task.FromResult(all);
    }
    public Task<bool> DeleteAsync(string id)
    {
        var removed = _store.Remove(id);
        if(removed)
        {
            _logger.LogWarning("Deleted Pharmacies {PharmaciesId}",id);
        }
        else
        {
            _logger.LogWarning("Deleted faild. Pharmacies {PharmaciesId}",id);
        }
        return Task.FromResult(removed);
    }





}

public record PharmaciesRecord(
    string Id,
    string Name,
    string LicenceNumber,
    int PhoneNumber,
    string Email,
    bool IsVerified,
    bool IsActive,
    decimal ReliablityScore,
    int FreshnessThreshold,
    DateTime LastinventoryUpdateAt,
    DateTime RegisteredAt
);