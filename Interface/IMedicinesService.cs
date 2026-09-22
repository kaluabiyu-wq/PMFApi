public interface IMedicinesService
{
  Task<MedicinesRecord> CreateAsync(string genericname,string brandname,string category
  ,string dosegeform,string strength,bool requeirsprescription,bool isactive);

  Task<MedicinesRecord?> GetByIdAsync(string id);

  Task<IReadOnlyList<MedicinesRecord>> GetAllAsync();

  Task<bool> DeleteAsync (string id); 

}