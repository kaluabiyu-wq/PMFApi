

namespace PmfApi.Application.Dtos;


public record PharmacyScheduleResponse
(
    int Id,
    int PharmacyId,
    int DayOfWeek,
    string OpenTime,
    string ClosedTime,
    bool IsClosed
);