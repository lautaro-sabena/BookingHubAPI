using BookingHubAPI.Application.Common;
using BookingHubAPI.Application.DTOs;

namespace BookingHubAPI.Application.Services;

/// <summary>Company use cases. Expected failures are returned as <see cref="Result"/> errors, never thrown.</summary>
public interface ICompanyService
{
    /// <summary>Returns the company owned by the caller; customers are forbidden.</summary>
    Task<Result<CompanyResponse>> GetMyCompanyAsync(Guid userId);

    /// <summary>Creates the caller's company; only an owner without one may.</summary>
    Task<Result<CompanyResponse>> CreateCompanyAsync(Guid userId, CompanyRequest request);

    /// <summary>Applies the non-empty fields of the request to the caller's own company.</summary>
    Task<Result<CompanyResponse>> UpdateMyCompanyAsync(Guid userId, CompanyUpdateRequest request);
}
