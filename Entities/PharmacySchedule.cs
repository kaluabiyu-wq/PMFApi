

namespace PmfApi.Entities;

public class PharmaciesSchedule
{
    public int Id {get;set;}

    public int PharmacyId {get;set;}

    public required int DayOfWeek {get;set;}

    public DateTime OpenTime {get;set;} = DateTime.UtcNow;
    public DateTime ClosedTime {get;set;} = DateTime.UtcNow;

    public bool ISClosed {get;set;} = false;

   public Pharmacy Pharmacy {get;set;} = null!;


}