


public class UserService : IUserService
{
  private readonly Dictionary<string, UserRecord> _store = new ();

  private readonly ILogger<UserService> _logger;

  public UserService(ILogger<UserService> logger)
    {
        _logger = logger;
    }

    public Task<UserRecord> CreateAsync(string fullName,
    decimal email,
    decimal password,
    string roleID,
    string locationID,
    bool isActive,
    bool createdAt)
    {
     var existing = _store.Values
     .FirstOrDefault(
            u => u.Email == email && u.Password == password);
     if(existing is not null)
        {
            _logger.LogWarning(
                "Duplicate User {FullName} {Email} already exitst in {UserId}",
                email,password,existing.Id);
            return Task.FromResult(existing);
           
        }

        var id = Guid.NewGuid().ToString("N")[..8];
        var user = new UserRecord(id,fullName,email,
       password, roleID, locationID,isActive,createdAt);
        _store[id] =  user;

        _logger.LogInformation(
            "Created User {FullName} {Email} {Password} {roleID} {locationID} {isActive} {createdAt} record {UserId}",fullName,email,
       password, roleID, locationID,isActive,createdAt,id);

         return Task.FromResult(user);
    }

    public Task<UserRecord?> GetByIdAsync(string id)
    {
        _store.TryGetValue(id,out var User);
        if (User is null)
        {
            _logger.LogWarning(
                "User {UserId} not found",id
            );
        }
        return Task.FromResult(User);
    }



    public Task<IReadOnlyList<UserRecord>> GetAllAsync()
    {
        IReadOnlyList<UserRecord> all = _store.Values.ToList();
        return Task.FromResult(all);
    }

    public Task<bool> DeleteAsync(string id)
    {
        var removed = _store.Remove(id);
        if(removed)
        {
            _logger.LogInformation("Deleted User {UserId}",id);
        }
        else
        {
            _logger.LogWarning(
                "Deleted failed. User {UserId} not Found",id);
        }
        return Task.FromResult(removed);
    }

}

public record UserRecord(
    string Id,
    string FullName,
    decimal Email,
    decimal Password,
    string RoleID,
    string LocationID,
    bool IsActive,
    bool CreatedAt
);