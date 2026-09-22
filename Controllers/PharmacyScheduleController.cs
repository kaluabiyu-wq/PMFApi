
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/pharmaciesSchedule")]


public class PharmaciesSchedulerController(IPharmaciesScheduleService pharmaciesScheduleService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var pharmaciesSchedules = await pharmaciesScheduleService.GetAllAsync();
        return Ok(pharmaciesSchedules);
    }
    
    [HttpGet("id")]
    public async Task<IActionResult> GetById(string id)
    {
        var pharmaciesSchedules = await pharmaciesScheduleService.GetByIdAsync(id);
        return pharmaciesSchedules is not null ? Ok(pharmaciesSchedules): NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreatePharmaciesScheduleRequest request)
    {
        var pharmaciesSchedules = await pharmaciesScheduleService.CreateAsync(
            request.PharmacyId,
            request.DayOfWeek,
            request.OpenTime,
            request.ClosedTime,
            request.IsClosed
       
   
   );

    return CreatedAtAction(
        nameof(GetById),
        new { id = pharmaciesSchedules.Id},pharmaciesSchedules);
    }
    [HttpDelete("id")]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await pharmaciesScheduleService.DeleteAsync(id);

        return deleted ? NoContent () :NotFound();
    }
    

   public record CreatePharmaciesScheduleRequest(
    string  PharmacyId ,
    int DayOfWeek,
    DateTime OpenTime,
    DateTime ClosedTime,
    bool IsClosed
   
);


}