using BookingHubAPI.Application.DTOs;
using BookingHubAPI.Domain.Entities;
using BookingHubAPI.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BookingHubAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WorkingHoursController : ControllerBase
{
    private readonly IWorkingHoursRepository _workingHoursRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IUserRepository _userRepository;

    public WorkingHoursController(
        IWorkingHoursRepository workingHoursRepository,
        ICompanyRepository companyRepository,
        IUserRepository userRepository)
    {
        _workingHoursRepository = workingHoursRepository;
        _companyRepository = companyRepository;
        _userRepository = userRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<WorkingHoursResponse>>> GetWorkingHours()
    {
        var userId = GetUserId();
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null || user.Role != UserRole.Owner || !user.CompanyId.HasValue)
        {
            return Forbid();
        }

        var workingHours = await _workingHoursRepository.GetByCompanyIdAsync(user.CompanyId.Value);

        var allDays = Enum.GetValues<DayOfWeek>()
            .Select(day => new WorkingHoursResponse(
                day,
                workingHours.FirstOrDefault(wh => wh.DayOfWeek == day)?.StartTime ?? new TimeSpan(9, 0, 0),
                workingHours.FirstOrDefault(wh => wh.DayOfWeek == day)?.EndTime ?? new TimeSpan(17, 0, 0),
                workingHours.Any(wh => wh.DayOfWeek == day && wh.IsActive)
            ));

        return Ok(allDays);
    }

    [HttpPut]
    public async Task<ActionResult<IEnumerable<WorkingHoursResponse>>> UpdateWorkingHours([FromBody] List<WorkingHoursRequest> requests)
    {
        var userId = GetUserId();
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null || user.Role != UserRole.Owner || !user.CompanyId.HasValue)
        {
            return Forbid();
        }

        var companyId = user.CompanyId.Value;

        await _workingHoursRepository.DeleteByCompanyIdAsync(companyId);

        foreach (var request in requests)
        {
            if (request.IsActive)
            {
                var workingHours = new WorkingHours
                {
                    CompanyId = companyId,
                    DayOfWeek = request.DayOfWeek,
                    StartTime = request.StartTime,
                    EndTime = request.EndTime,
                    IsActive = request.IsActive
                };
                await _workingHoursRepository.CreateAsync(workingHours);
            }
        }

        return Ok(await GetWorkingHours());
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(userIdClaim!);
    }
}
