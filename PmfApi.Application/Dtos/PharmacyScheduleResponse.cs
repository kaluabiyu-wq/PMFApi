

namespace PmfApi.Application.Dtos;


public record PharmacyScheduleResponse
(
    int Id,
    int PharmacyId,
    int DayOfWeek,
    DateTime OpenTime,
    DateTime ClosedTime,
    bool IsClosed
);