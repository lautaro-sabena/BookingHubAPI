using BookingHubAPI.Application.Common;
using BookingHubAPI.Application.DTOs;
using BookingHubAPI.Domain.Entities;
using BookingHubAPI.Domain.Interfaces;

namespace BookingHubAPI.Application.Services;

public class WorkingHoursService : IWorkingHoursService
{
    private static readonly TimeSpan DefaultStartTime = new(9, 0, 0);
    private static readonly TimeSpan DefaultEndTime = new(17, 0, 0);

    private readonly IWorkingHoursRepository _workingHoursRepository;
    private readonly IUserRepository _userRepository;

    public WorkingHoursService(
        IWorkingHoursRepository workingHoursRepository,
        IUserRepository userRepository)
    {
        _workingHoursRepository = workingHoursRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<IReadOnlyList<WorkingHoursResponse>>> GetWorkingHoursAsync(Guid userId)
    {
        var companyId = await GetOwnCompanyIdAsync(userId);
        if (companyId.IsFailure)
        {
            return companyId.Error!;
        }

        var workingHours = (await _workingHoursRepository.GetByCompanyIdAsync(companyId.Value)).ToList();

        IReadOnlyList<WorkingHoursResponse> allDays = Enum.GetValues<DayOfWeek>()
            .Select(day => new WorkingHoursResponse(
                day,
                workingHours.FirstOrDefault(wh => wh.DayOfWeek == day)?.StartTime ?? DefaultStartTime,
                workingHours.FirstOrDefault(wh => wh.DayOfWeek == day)?.EndTime ?? DefaultEndTime,
                workingHours.Any(wh => wh.DayOfWeek == day && wh.IsActive)))
            .ToList();

        return Result<IReadOnlyList<WorkingHoursResponse>>.Success(allDays);
    }

    public async Task<Result> ReplaceWorkingHoursAsync(Guid userId, IReadOnlyList<WorkingHoursRequest> requests)
    {
        var companyId = await GetOwnCompanyIdAsync(userId);
        if (companyId.IsFailure)
        {
            return Result.Failure(companyId.Error!);
        }

        await _workingHoursRepository.DeleteByCompanyIdAsync(companyId.Value);

        foreach (var request in requests.Where(r => r.IsActive))
        {
            await _workingHoursRepository.CreateAsync(new WorkingHours
            {
                CompanyId = companyId.Value,
                DayOfWeek = request.DayOfWeek,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                IsActive = request.IsActive
            });
        }

        return Result.Success();
    }

    private async Task<Result<Guid>> GetOwnCompanyIdAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null || user.Role != UserRole.Owner || !user.CompanyId.HasValue)
        {
            return Error.Forbidden("Only an owner with a company can manage working hours");
        }

        return user.CompanyId.Value;
    }
}
