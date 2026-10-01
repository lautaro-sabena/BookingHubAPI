using BookingHubAPI.Application.Common;
using BookingHubAPI.Application.DTOs;
using BookingHubAPI.Domain.Entities;
using BookingHubAPI.Domain.Interfaces;

namespace BookingHubAPI.Application.Services;

public class CompanyService : ICompanyService
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IUserRepository _userRepository;

    public CompanyService(ICompanyRepository companyRepository, IUserRepository userRepository)
    {
        _companyRepository = companyRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<CompanyResponse>> GetMyCompanyAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return Error.NotFound("User not found");
        }

        if (user.Role == UserRole.Customer)
        {
            return Error.Forbidden("Customers do not own a company");
        }

        var company = await _companyRepository.GetByOwnerIdAsync(userId);
        if (company == null)
        {
            return Error.NotFound("Company not found");
        }

        return ToResponse(company);
    }

    public async Task<Result<CompanyResponse>> CreateCompanyAsync(Guid userId, CompanyRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return Error.NotFound("User not found");
        }

        if (user.Role != UserRole.Owner)
        {
            return Error.Forbidden("Only an owner can create a company");
        }

        if (await _companyRepository.GetByOwnerIdAsync(userId) != null)
        {
            return Error.Validation("You already have a company");
        }

        if (!IsKnownTimeZone(request.TimeZone))
        {
            return Error.Validation(InvalidTimeZoneMessage);
        }

        var company = new Company
        {
            Name = request.Name,
            Description = request.Description,
            TimeZone = request.TimeZone,
            OwnerId = userId
        };

        var created = await _companyRepository.CreateAsync(company);

        user.CompanyId = created.Id;
        await _userRepository.UpdateAsync(user);

        return ToResponse(created);
    }

    public async Task<Result<CompanyResponse>> UpdateMyCompanyAsync(Guid userId, CompanyUpdateRequest request)
    {
        var company = await _companyRepository.GetByOwnerIdAsync(userId);
        if (company == null)
        {
            return Error.NotFound("Company not found");
        }

        if (!string.IsNullOrEmpty(request.TimeZone) && !IsKnownTimeZone(request.TimeZone))
        {
            return Error.Validation(InvalidTimeZoneMessage);
        }

        if (!string.IsNullOrEmpty(request.Name))
        {
            company.Name = request.Name;
        }

        if (request.Description != null)
        {
            company.Description = request.Description;
        }

        if (!string.IsNullOrEmpty(request.TimeZone))
        {
            company.TimeZone = request.TimeZone;
        }

        return ToResponse(await _companyRepository.UpdateAsync(company));
    }

    private const string InvalidTimeZoneMessage = "Invalid time zone";

    private static bool IsKnownTimeZone(string timeZoneId) =>
        TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out _);

    private static CompanyResponse ToResponse(Company c) =>
        new(c.Id, c.Name, c.Description, c.TimeZone, c.IsActive, c.OwnerId);
}
