using BookingHubAPI.Application.Common;
using BookingHubAPI.Application.DTOs;
using BookingHubAPI.Domain.Entities;
using BookingHubAPI.Domain.Interfaces;
using ServiceEntity = BookingHubAPI.Domain.Entities.Service;

namespace BookingHubAPI.Application.Services;

public class ServiceCatalogService : IServiceCatalogService
{
    private readonly IServiceRepository _serviceRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IUserRepository _userRepository;

    public ServiceCatalogService(
        IServiceRepository serviceRepository,
        ICompanyRepository companyRepository,
        IUserRepository userRepository)
    {
        _serviceRepository = serviceRepository;
        _companyRepository = companyRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<PagedResult<ServiceResponse>>> GetOwnServicesAsync(
        Guid userId, int page, int pageSize, string? search)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return Error.NotFound("User not found");
        }

        if (!IsOwnerWithCompany(user))
        {
            return Error.Forbidden("Only an owner with a company can list its services");
        }

        var companyId = user.CompanyId!.Value;
        var services = await _serviceRepository.GetByCompanyIdAsync(companyId, page, pageSize, search);
        var totalCount = await _serviceRepository.GetCountByCompanyIdAsync(companyId, search);

        return ToPage(services, totalCount, page, pageSize);
    }

    public async Task<PagedResult<ServiceResponse>> GetPublicServicesAsync(int page, int pageSize)
    {
        var services = await _serviceRepository.GetAllActiveAsync(page, pageSize);
        var totalCount = await _serviceRepository.GetAllActiveCountAsync();

        return ToPage(services, totalCount, page, pageSize);
    }

    public async Task<Result<ServiceResponse>> GetServiceAsync(Guid userId, Guid serviceId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return Error.NotFound("User not found");
        }

        var service = await _serviceRepository.GetByIdWithCompanyAsync(serviceId);
        if (service == null)
        {
            return Error.NotFound("Service not found");
        }

        var isOwnCompany = IsOwnerWithCompany(user) && service.CompanyId == user.CompanyId!.Value;
        if (isOwnCompany || (service.IsActive && service.Company.IsActive))
        {
            return ToResponse(service, service.Company.Name, service.Company.Description);
        }

        return Error.Forbidden("Service is not available");
    }

    public async Task<Result<ServiceResponse>> CreateServiceAsync(Guid userId, ServiceRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null || !IsOwnerWithCompany(user))
        {
            return Error.Forbidden("Only an owner with a company can create services");
        }

        var service = new ServiceEntity
        {
            Name = request.Name,
            Description = request.Description,
            DurationMinutes = request.DurationMinutes,
            Price = request.Price,
            CompanyId = user.CompanyId!.Value
        };

        var created = await _serviceRepository.CreateAsync(service);

        return await ToResponseWithCompanyAsync(created);
    }

    public async Task<Result<ServiceResponse>> UpdateServiceAsync(
        Guid userId, Guid serviceId, ServiceUpdateRequest request)
    {
        var found = await FindOwnServiceAsync(userId, serviceId);
        if (found.IsFailure)
        {
            return found.Error!;
        }

        var service = found.Value;

        if (!string.IsNullOrEmpty(request.Name))
        {
            service.Name = request.Name;
        }

        if (request.Description != null)
        {
            service.Description = request.Description;
        }

        if (request.DurationMinutes.HasValue)
        {
            service.DurationMinutes = request.DurationMinutes.Value;
        }

        if (request.Price.HasValue)
        {
            service.Price = request.Price.Value;
        }

        if (request.IsActive.HasValue)
        {
            service.IsActive = request.IsActive.Value;
        }

        var updated = await _serviceRepository.UpdateAsync(service);

        return await ToResponseWithCompanyAsync(updated);
    }

    public async Task<Result> DeleteServiceAsync(Guid userId, Guid serviceId)
    {
        var found = await FindOwnServiceAsync(userId, serviceId);
        if (found.IsFailure)
        {
            return Result.Failure(found.Error!);
        }

        found.Value.IsActive = false;
        await _serviceRepository.UpdateAsync(found.Value);

        return Result.Success();
    }

    /// <summary>
    /// Loads a service for modification. A service of another company is reported as not found,
    /// exactly like a missing one, so its existence is not disclosed.
    /// </summary>
    private async Task<Result<ServiceEntity>> FindOwnServiceAsync(Guid userId, Guid serviceId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null || !IsOwnerWithCompany(user))
        {
            return Error.Forbidden("Only an owner with a company can manage services");
        }

        var service = await _serviceRepository.GetByIdAsync(serviceId);
        if (service == null || service.CompanyId != user.CompanyId!.Value)
        {
            return Error.NotFound("Service not found");
        }

        return service;
    }

    private static bool IsOwnerWithCompany(User user) => user.Role == UserRole.Owner && user.CompanyId.HasValue;

    private async Task<ServiceResponse> ToResponseWithCompanyAsync(ServiceEntity service)
    {
        var company = await _companyRepository.GetByIdAsync(service.CompanyId);
        return ToResponse(service, company?.Name ?? string.Empty, company?.Description);
    }

    private static ServiceResponse ToResponse(ServiceEntity s, string companyName, string? companyDescription) =>
        new(s.Id, s.Name, s.Description, s.DurationMinutes, s.Price, s.IsActive, s.CompanyId, companyName, companyDescription);

    private static PagedResult<ServiceResponse> ToPage(
        IEnumerable<ServiceEntity> services, int totalCount, int page, int pageSize)
    {
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        var items = services.Select(s => ToResponse(s, s.Company.Name, s.Company.Description)).ToList();
        return new PagedResult<ServiceResponse>(items, totalCount, page, pageSize, totalPages);
    }
}
