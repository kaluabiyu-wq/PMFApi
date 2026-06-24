public interface IUserService
{
Task<UserRecord> CreateAsync(
    string fullName,
    decimal email,
    decimal password,
    string roleID,
    string locationID,
    bool isActive,
    bool createdAt);
Task<UserRecord?> GetByIdAsync(string id);

Task<IReadOnlyList<UserRecord>> GetAllAsync();

Task<bool> DeleteAsync(string id);

}