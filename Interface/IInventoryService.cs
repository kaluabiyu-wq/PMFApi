
public interface IInventoryService
{
 Task<InventoryRecord> CreateAsync(string medicineId,string pharmacyId,decimal price);
Task<InventoryRecord?> GetByIdAsync(string id);

Task<IReadOnlyList<InventoryRecord>> GetAllAsync();

Task<bool> DeleteAsync(string id);
    
}





