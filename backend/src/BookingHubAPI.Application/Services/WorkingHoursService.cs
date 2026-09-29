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

        var configured = workingHours
            .Select(wh => new WorkingHoursResponse(wh.DayOfWeek, wh.StartTime, wh.EndTime, wh.IsActive))
            .ToList();

        // Legacy data may hold several rows for one day: times come from the first, the day is active if any is.
        return Result<IReadOnlyList<WorkingHoursResponse>>.Success(ToWeek(
            configured, day => workingHours.Any(wh => wh.DayOfWeek == day && wh.IsActive)));
    }

    public async Task<Result<IReadOnlyList<WorkingHoursResponse>>> ReplaceWorkingHoursAsync(
        Guid userId, IReadOnlyList<WorkingHoursRequest> requests)
    {
        var companyId = await GetOwnCompanyIdAsync(userId);
        if (companyId.IsFailure)
        {
            return companyId.Error!;
        }

        var validationError = Validate(requests);
        if (validationError != null)
        {
            return validationError;
        }

        // Not atomic: delete-then-insert are separate writes (there is no unit of work yet). Tracked for T9.
        await _workingHoursRepository.DeleteByCompanyIdAsync(companyId.Value);

        // Inactive days are stored too, so the times the owner set for a disabled day survive.
        foreach (var request in requests)
        {
            await _workingHoursRepository.CreateAsync(WorkingHours.Create(
                companyId.Value, request.DayOfWeek, request.StartTime, request.EndTime, request.IsActive));
        }

        var saved = requests
            .Select(r => new WorkingHoursResponse(r.DayOfWeek, r.StartTime, r.EndTime, r.IsActive))
            .ToList();
        return Result<IReadOnlyList<WorkingHoursResponse>>.Success(ToWeek(
            saved, day => saved.Any(s => s.DayOfWeek == day && s.IsActive)));
    }

    private static Error? Validate(IReadOnlyList<WorkingHoursRequest> requests)
    {
        foreach (var request in requests)
        {
            if (!Enum.IsDefined(request.DayOfWeek))
            {
                return Error.Validation($"Invalid day of week: {(int)request.DayOfWeek}");
            }
        }

        var duplicate = requests.GroupBy(r => r.DayOfWeek).FirstOrDefault(g => g.Count() > 1);
        if (duplicate != null)
        {
            return Error.Validation($"Duplicate working hours for {duplicate.Key}");
        }

        // The time window rules (within one day, start before end on active days) belong to the entity.
        foreach (var request in requests)
        {
            var problem = WorkingHours.Validate(request.DayOfWeek, request.StartTime, request.EndTime, request.IsActive);
            if (problem != null)
            {
                return Error.Validation(problem);
            }
        }

        return null;
    }

    /// <summary>One entry per weekday: the first configured entry of the day, else the inactive default window.</summary>
    private static IReadOnlyList<WorkingHoursResponse> ToWeek(
        IReadOnlyCollection<WorkingHoursResponse> configured, Func<DayOfWeek, bool> isActive) =>
        Enum.GetValues<DayOfWeek>()
            .Select(day =>
            {
                var entry = configured.FirstOrDefault(c => c.DayOfWeek == day);
                return new WorkingHoursResponse(
                    day, entry?.StartTime ?? DefaultStartTime, entry?.EndTime ?? DefaultEndTime, isActive(day));
            })
            .ToList();

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
