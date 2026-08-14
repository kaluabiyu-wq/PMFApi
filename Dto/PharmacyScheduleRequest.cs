

namespace PmfApi.Dto;

public record PharmaciesScheduleRequest
{
    
    public required int PharmacyId {get;set;}

    public required int DayOfWeek {get;set;}

    public DateTime OpenTime {get;set;} = DateTime.UtcNow;
    public DateTime ClosedTime {get;set;} = DateTime.UtcNow;

}