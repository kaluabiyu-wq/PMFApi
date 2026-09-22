

public class InventoryService : IInventoryService
{
  private readonly Dictionary<string, InventoryRecord> _store = new ();

  private readonly ILogger<InventoryService> _logger;

  public InventoryService(ILogger<InventoryService> logger)
    {
        _logger = logger;
    }

    public Task<InventoryRecord> CreateAsync(string medicineId,string pharmacyId,decimal price)
    {
     var existing = _store.Values
     .FirstOrDefault(
            l => l.MedicineId == medicineId && l.PharmacyId == pharmacyId);
     if(existing is not null)
        {
            _logger.LogWarning(
                "Duplicate Inventory {MedicineId} {PharmacyId} already exitst in {InventoryId}",
                medicineId,pharmacyId,existing.Id);
            return Task.FromResult(existing);
           
        }

        var id = Guid.NewGuid().ToString("N")[..8];
        var inventory = new InventoryRecord(id,medicineId,pharmacyId,price,DateTime.UtcNow);
        _store[id] = inventory ;

        _logger.LogInformation(
            "Created Inventory {MedicineId}  {PharmacyId} {Price}  record {InventoryId}",
            medicineId,pharmacyId,price,id);

         return Task.FromResult(inventory);
    }

    public Task<InventoryRecord?> GetByIdAsync(string id)
    {
        _store.TryGetValue(id,out var inventory);
        if (inventory is null)
        {
            _logger.LogWarning(
                "Inventory {InventoryId} not found",id
            );
        }
        return Task.FromResult(inventory);
    }



    public Task<IReadOnlyList<InventoryRecord>> GetAllAsync()
    {
        IReadOnlyList<InventoryRecord> all = _store.Values.ToList();
        return Task.FromResult(all);
    }

    public Task<bool> DeleteAsync(string id)
    {
        var removed = _store.Remove(id);
        if(removed)
        {
            _logger.LogInformation("Deleted Inventory {InventoryId}",id);
        }
        else
        {
            _logger.LogWarning(
                "Deleted failed. Inventory {InventoryId} not Found",id);
        }
        return Task.FromResult(removed);
    }

}
public record InventoryRecord(
    string Id,
    string MedicineId,
    string PharmacyId,
    decimal Price,
    DateTime LastUpdateAt
);