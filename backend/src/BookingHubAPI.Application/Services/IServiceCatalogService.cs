using BookingHubAPI.Application.Common;
using BookingHubAPI.Application.DTOs;

namespace BookingHubAPI.Application.Services;

/// <summary>Service catalog use cases. Expected failures are returned as <see cref="Result"/> errors, never thrown.</summary>
public interface IServiceCatalogService
{
    /// <summary>Lists the active services of the owner's own company, optionally filtered by name.</summary>
    Task<Result<PagedResult<ServiceResponse>>> GetOwnServicesAsync(
        Guid userId, int page, int pageSize, string? search);

    /// <summary>Lists the active services of active companies, for any caller.</summary>
    Task<PagedResult<ServiceResponse>> GetPublicServicesAsync(int page, int pageSize);

    /// <summary>
    /// Returns a service to its own company's owner, or to anyone else when both the service
    /// and its company are active.
    /// </summary>
    Task<Result<ServiceResponse>> GetServiceAsync(Guid userId, Guid serviceId);

    Task<Result<ServiceResponse>> CreateServiceAsync(Guid userId, ServiceRequest request);

    /// <summary>Applies the non-null fields of the request to a service of the owner's company.</summary>
    Task<Result<ServiceResponse>> UpdateServiceAsync(Guid userId, Guid serviceId, ServiceUpdateRequest request);

    /// <summary>Soft-deletes (deactivates) a service of the owner's company.</summary>
    Task<Result> DeleteServiceAsync(Guid userId, Guid serviceId);
}
