public interface IUserFeedBackService
{
    Task<UserFeedBackRecord> CreateAsync (string userId,string inventoryId,
    string pharmacyId,bool wasMedicineAvailable,DateTime submittedAt,string comments);

    Task<UserFeedBackRecord?> GetByIdAsync(string id);

    Task<IReadOnlyList<UserFeedBackRecord>> GetAllAsync ();

    Task<bool> DeleteAsync(string id);
}