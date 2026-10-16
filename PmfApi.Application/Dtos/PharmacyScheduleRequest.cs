namespace PmfApi.Application.Dtos;


public record PharmaciesScheduleRequest
{
    public required int DayOfWeek { get; init; }

    public bool IsClosed { get; init; }

    public string OpenTime { get; init; } = "08:00";

    public string ClosedTime { get; init; } = "20:00";
}