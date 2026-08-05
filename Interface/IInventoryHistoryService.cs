
public interface IInventoryHistoryService
{
 Task<InventoryHistoryRecord> CreateAsync(string inventoryId,string pharmacyId,string medicineId,decimal oldprice);
Task<InventoryHistoryRecord?> GetByIdAsync(string id);

Task<IReadOnlyList<InventoryHistoryRecord>> GetAllAsync();

Task<bool> DeleteAsync(string id);
    
}





