public interface IPharmaciesScheduleService
{
   Task<PharmaciesScheduleRecord> CreateAsync(string  pharmacyId , int dayOfWeek,
    DateTime openTime,DateTime closedTime,bool isClosed);

   Task<PharmaciesScheduleRecord?> GetByIdAsync(string id);

   Task<IReadOnlyList<PharmaciesScheduleRecord>> GetAllAsync();

   Task<bool> DeleteAsync(string id);

    
}
