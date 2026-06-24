


public class UserFeedBackService : IUserFeedBackService
{
    private readonly Dictionary<string, UserFeedBackRecord> _store = new();

    private readonly ILogger<UserFeedBackService> _logger;

    public  UserFeedBackService(ILogger<UserFeedBackService> logger)
    {
         _logger = logger;
    }

  public Task<UserFeedBackRecord> CreateAsync(
    string userId,string inventoryId,
    string pharmacyId,bool wasMedicineAvailable,DateTime submittedAt,string comments
  )
    {
        var existing = _store.Values
        .FirstOrDefault(uf => uf.UserId == userId && uf.InventoryId == inventoryId);
        if(existing is not null)
        {

  _logger.LogWarning(
    "Duplicate UserFeedback {UserId} {InventoryId} in record (in {UserFeedBackId})",
            userId,inventoryId,existing.Id );
        return Task.FromResult(existing);
        
        }
          var id = Guid.NewGuid().ToString("N")[..8];
        var userfeedback = new UserFeedBackRecord(id,userId,inventoryId,
           pharmacyId,wasMedicineAvailable,submittedAt,comments);

           _store[id] = userfeedback;

           _logger.LogInformation(

 "Created User Feedback {UserId} {InventoryId} {PharmacyId} {WasMedicineAvailable} {submittedAt} {comments}  record {UserFeedBackId}",
    userId,inventoryId,
           pharmacyId,wasMedicineAvailable,submittedAt,comments,id
  );
     return Task.FromResult(userfeedback);
    }

public Task<UserFeedBackRecord?> GetByIdAsync(string id)
    {
         _store.TryGetValue(id, out var userFeedBack);

        if (userFeedBack is null )
        {
            _logger.LogWarning("UserFeedback {UserFeedBackId} not found",id);
        }

        return Task.FromResult(userFeedBack);
    }

 public Task<IReadOnlyList<UserFeedBackRecord>> GetAllAsync()
    {
        IReadOnlyList<UserFeedBackRecord> all = _store.Values.ToList();
        return Task.FromResult(all);
    }

    public Task<bool> DeleteAsync(string id)
    {
        var removed =_store.Remove(id);

        if(removed)
        {
            _logger.LogInformation("Deleted User Feedback {UserFeedBackId}",id);
        }
        else
        {
            _logger.LogWarning("Deleted failed. User Feedback {UserFeedBackId} not found",id);
        }
        return Task.FromResult(removed);
    }





    }





public record UserFeedBackRecord(
   string Id,
   string UserId,
   string InventoryId,
  string PharmacyId,
  bool WasMedicineAvailable,
  DateTime SubmittedAt,
  string Comments
);