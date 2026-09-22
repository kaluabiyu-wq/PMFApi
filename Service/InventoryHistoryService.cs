
public class InventoryHistoryService : IInventoryHistoryService
{
  private readonly Dictionary<string, InventoryHistoryRecord> _store = new ();

  private readonly ILogger<InventoryHistoryService> _logger;

  public InventoryHistoryService(ILogger<InventoryHistoryService> logger)
    {
        _logger = logger;
    }

    public Task<InventoryHistoryRecord> CreateAsync(string inventoryId,string medicineId,string pharmacyId,decimal oldprice)
    {
     var existing = _store.Values
     .FirstOrDefault(
            l => l.InventoryId == inventoryId && l.PharmacyId == pharmacyId && l.MedicineId == medicineId);
     if(existing is not null)
        {
            _logger.LogWarning(
                "Duplicate Inventory History {InventoryId} {PharmacyId} {MedicineId} already exitst in {InventoryId}",
                medicineId,pharmacyId,medicineId,existing.Id);
            return Task.FromResult(existing);
           
        }

        var id = Guid.NewGuid().ToString("N")[..8];
        var inventoryhistory = new InventoryHistoryRecord(id,inventoryId,pharmacyId,medicineId,oldprice,DateTime.UtcNow);
        _store[id] = inventoryhistory ;

        _logger.LogInformation(
            "Created Inventory {InventoryId} {PharmacyId} {MedicineId} {OldPrice}  record {InventoryHistoryId}",
            inventoryId,medicineId,pharmacyId,oldprice,id);

         return Task.FromResult(inventoryhistory);
    }

    public Task<InventoryHistoryRecord?> GetByIdAsync(string id)
    {
        _store.TryGetValue(id,out var inventoryhistory);
        if (inventoryhistory is null)
        {
            _logger.LogWarning(
                "Inventory History {InventoryHistoryId} not found",id
            );
        }
        return Task.FromResult(inventoryhistory);
    }



    public Task<IReadOnlyList<InventoryHistoryRecord>> GetAllAsync()
    {
        IReadOnlyList<InventoryHistoryRecord> all = _store.Values.ToList();
        return Task.FromResult(all);
    }

    public Task<bool> DeleteAsync(string id)
    {
        var removed = _store.Remove(id);
        if(removed)
        {
            _logger.LogInformation("Deleted Inventory History {InventoryHistoryId}",id);
        }
        else
        {
            _logger.LogWarning(
                "Deleted failed. Inventory History {InventoryHistoryId} not Found",id);
        }
        return Task.FromResult(removed);
    }

}

public record InventoryHistoryRecord(
    string Id,
    string InventoryId,
    string MedicineId,
    string PharmacyId,
    decimal OldPrice,
    DateTime ChangedAt
);