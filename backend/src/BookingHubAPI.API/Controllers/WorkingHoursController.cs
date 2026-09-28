using BookingHubAPI.Application.DTOs;
using BookingHubAPI.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BookingHubAPI.API.Extensions;

namespace BookingHubAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WorkingHoursController : ControllerBase
{
    private readonly IWorkingHoursService _workingHoursService;

    public WorkingHoursController(IWorkingHoursService workingHoursService)
    {
        _workingHoursService = workingHoursService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<WorkingHoursResponse>>> GetWorkingHours()
    {
        var result = await _workingHoursService.GetWorkingHoursAsync(User.GetUserId());
        return this.ToActionResult(result, Ok);
    }

    [HttpPut]
    public async Task<ActionResult<IEnumerable<WorkingHoursResponse>>> UpdateWorkingHours([FromBody] List<WorkingHoursRequest> requests)
    {
        var result = await _workingHoursService.ReplaceWorkingHoursAsync(User.GetUserId(), requests);
        if (result.IsFailure)
        {
            return this.ToFailureResult(result.Error!);
        }

        // CURRENT BEHAVIOR (bug): nests the GET action result instead of its value. Kept as-is so the
        // response body does not change (pinned by WorkingHoursControllerTests).
        return Ok(await GetWorkingHours());
    }
}
