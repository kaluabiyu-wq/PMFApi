

public class MedicinesService : IMedicinesService
{
    private readonly Dictionary<string, MedicinesRecord> _store = new();
    private readonly ILogger<MedicinesService> _logger;
    
    public MedicinesService(ILogger<MedicinesService> logger)
    {
        _logger = logger;
    }

    public Task<MedicinesRecord> CreateAsync(string genericname,string brandname,string category
  ,string dosegeform,string strength,bool requeirsprescription,bool isactive)
    {
        var existing = _store.Values
        .FirstOrDefault( m => m.GenericName == genericname && m.BrandName == brandname);
        if(existing is not null)
        {
            _logger.LogWarning(
                "Dupliacte Medicinies {GenericName} {BrandName} already exists (record {MedicinesId})",
                genericname,brandname,existing.Id
            );
            return Task.FromResult(existing);
        }
     var id = Guid.NewGuid().ToString("N")[..8];

     var medicinies = new MedicinesRecord
     (id,genericname, brandname,category
  ,dosegeform,strength,requeirsprescription,isactive);

  _store[id] = medicinies;

  _logger.LogInformation(
    "Created Medicinies {GenericName} {BrandName} {Category} {DosegeForm}v{Strength} {RequerieScription} {IsActive} record {MediciniesId}",
    genericname, brandname,category
  ,dosegeform,strength,requeirsprescription,isactive,id
  );
     return Task.FromResult(medicinies);
    }

public Task<MedicinesRecord?> GetByIdAsync(string id)
    {
        _store.TryGetValue(id, out var medicines);

        if (medicines is null )
        {
            _logger.LogWarning("Medicinies {MedicineId} not found",id);
        }

        return Task.FromResult(medicines);
    }

    public Task<IReadOnlyList<MedicinesRecord>> GetAllAsync()
    {
        IReadOnlyList<MedicinesRecord> all = _store.Values.ToList();
        return Task.FromResult(all);
    }

    public Task<bool> DeleteAsync(string id)
    {
        var removed =_store.Remove(id);

        if(removed)
        {
            _logger.LogInformation("Deleted Medicinies {MediciniesId}",id);
        }
        else
        {
            _logger.LogWarning("Deleted failed. Medicinies {MediciniesId} not found",id);
        }
        return Task.FromResult(removed);
    }



}



public record MedicinesRecord(
string Id,
string GenericName,
string BrandName,
string Category,
string DosageForm,
string Strength,
bool RequeiresPrescription,
bool IssActive
);

