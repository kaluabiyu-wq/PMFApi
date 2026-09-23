

namespace PmfApi.Dto;

public record PharmaciesScheduleRequest
{
    
    public required int PharmacyId {get;init;}

    public required int DayOfWeek {get;init;}

    public DateTime OpenTime {get;init;} = DateTime.UtcNow;
    public DateTime ClosedTime {get;init;} = DateTime.UtcNow;

}