using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PmfApi.Application.Dtos;
using PmfApi.Application.Interfaces;

namespace PmfApi.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/pharmacies/{pharmacyId:int}/schedule")]
[Tags("Pharmacy Schedules")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public class PharmaciesScheduleController(
    IPharmaciesScheduleService scheduleService,
    IPharmaciesService pharmaciesService,
    IAuthorizationService authorizationService) : ControllerBase
{
    [HttpGet(Name = nameof(GetWeek))]
    [ProducesResponseType(typeof(IReadOnlyList<PharmacyScheduleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Get a pharmacy's weekly opening hours")]
    [EndpointDescription("Returns one row per stored day, ordered Sunday (0) to Saturday (6). Any signed-in user may read it so patients can see opening hours.")]
    public async Task<IActionResult> GetWeek(int pharmacyId, CancellationToken ct)
    {
        var pharmacy = await pharmaciesService.GetEntityByIdAsync(pharmacyId, ct);
        if (pharmacy is null) return NotFound();

        return Ok(await scheduleService.GetWeekAsync(pharmacyId, ct));
    }

    [HttpGet("{id:int}", Name = nameof(GetByPhramacyId))]
    [ProducesResponseType(typeof(PharmacyScheduleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Get one schedule row for a pharmacy")]
    public async Task<IActionResult> GetByPhramacyId(int pharmacyId, int id, CancellationToken ct)
    {
        var row = await scheduleService.GetByPhramacyIdAsync(pharmacyId, id, ct);
        return row is not null ? Ok(row) : NotFound();
    }

    [HttpPut]
    [ProducesResponseType(typeof(IReadOnlyList<PharmacyScheduleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Save a pharmacy's weekly opening hours")]
    [EndpointDescription("Upserts the submitted days (matched by dayOfWeek) in one save and returns the full week. Only the pharmacy's own staff or a system admin may call it.")]
    public async Task<IActionResult> SaveWeek(
        int pharmacyId, [FromBody] List<PharmaciesScheduleRequest> days, CancellationToken ct)
    {
        var pharmacy = await pharmaciesService.GetEntityByIdAsync(pharmacyId, ct);
        if (pharmacy is null) return NotFound();

        var auth = await authorizationService.AuthorizeAsync(User, pharmacy, "PharmacyOwnerOrAdmin");
        if (!auth.Succeeded) return Forbid();

        var errors = Validate(days);
        if (errors.Count > 0) return ValidationProblem(new ValidationProblemDetails(errors));

        return Ok(await scheduleService.SaveWeekAsync(pharmacyId, days, ct));
    }

    private static Dictionary<string, string[]> Validate(List<PharmaciesScheduleRequest> days)
    {
        var errors = new Dictionary<string, string[]>();

        if (days.Count == 0)
        {
            errors["days"] = ["Send at least one day."];
            return errors;
        }

        if (days.Select(d => d.DayOfWeek).Distinct().Count() != days.Count)
            errors["days"] = ["Each day of the week may appear only once."];

        for (var i = 0; i < days.Count; i++)
        {
            var day = days[i];
            var key = $"days[{i}]";

            if (day.DayOfWeek is < 0 or > 6)
            {
                errors[$"{key}.dayOfWeek"] = ["dayOfWeek must be 0 (Sunday) to 6 (Saturday)."];
                continue;
            }

            var openOk = TimeOnly.TryParseExact(day.OpenTime, "HH:mm", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var open);
            var closeOk = TimeOnly.TryParseExact(day.ClosedTime, "HH:mm", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var close);

            if (!openOk) errors[$"{key}.openTime"] = ["Use 24-hour HH:mm, for example 08:00."];
            if (!closeOk) errors[$"{key}.closedTime"] = ["Use 24-hour HH:mm, for example 20:00."];

            if (openOk && closeOk && !day.IsClosed && close <= open)
                errors[$"{key}.closedTime"] = ["Closing time must be after opening time."];
        }

        return errors;
    }
}