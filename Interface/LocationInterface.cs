public interface ILocationService
{
Task<LocationRecord> CreateAsync(string label,decimal latitude,
decimal longitude,string subcity,
string woreda,string city);
Task<LocationRecord?> GetByIdAsync(string id);

Task<IReadOnlyList<LocationRecord>> GetAllAsync();

Task<bool> DeleteAsync(string id);


}