public interface IPharmaciesService
{
   Task<PharmaciesRecord> CreateAsync(string name,string LicenceNumber,int phoneNumber
   ,string email,bool isVerified,bool isActive,decimal relialbilityScore, int freshnessThreshold
  
   );

   Task<PharmaciesRecord?> GetByIdAsync(string id);

   Task<IReadOnlyList<PharmaciesRecord>> GetAllAsync();

   Task<bool> DeleteAsync(string id);

}