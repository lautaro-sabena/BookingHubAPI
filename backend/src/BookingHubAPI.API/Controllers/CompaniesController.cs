using BookingHubAPI.Application.DTOs;
using BookingHubAPI.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BookingHubAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CompaniesController : ControllerBase
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IUserRepository _userRepository;

    public CompaniesController(ICompanyRepository companyRepository, IUserRepository userRepository)
    {
        _companyRepository = companyRepository;
        _userRepository = userRepository;
    }

    [HttpGet("me")]
    public async Task<ActionResult<CompanyResponse>> GetMyCompany()
    {
        var userId = GetUserId();
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null)
        {
            return NotFound();
        }

        if (user.Role == Domain.Entities.UserRole.Customer)
        {
            return Forbid();
        }

        var company = await _companyRepository.GetByOwnerIdAsync(userId);

        if (company == null)
        {
            return NotFound();
        }

        return Ok(new CompanyResponse(company.Id, company.Name, company.Description, company.TimeZone, company.IsActive, company.OwnerId));
    }

    [HttpPost]
    public async Task<ActionResult<CompanyResponse>> CreateCompany([FromBody] CompanyRequest request)
    {
        var userId = GetUserId();
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null)
        {
            return NotFound();
        }

        if (user.Role != Domain.Entities.UserRole.Owner)
        {
            return Forbid();
        }

        var existingCompany = await _companyRepository.GetByOwnerIdAsync(userId);
        if (existingCompany != null)
        {
            return BadRequest(new { error = "You already have a company" });
        }

        var company = new Domain.Entities.Company
        {
            Name = request.Name,
            Description = request.Description,
            TimeZone = request.TimeZone,
            OwnerId = userId
        };

        var createdCompany = await _companyRepository.CreateAsync(company);

        user.CompanyId = createdCompany.Id;
        await _userRepository.UpdateAsync(user);

        return CreatedAtAction(nameof(GetMyCompany), new CompanyResponse(
            createdCompany.Id,
            createdCompany.Name,
            createdCompany.Description,
            createdCompany.TimeZone,
            createdCompany.IsActive,
            createdCompany.OwnerId));
    }

    [HttpPut("me")]
    public async Task<ActionResult<CompanyResponse>> UpdateMyCompany([FromBody] CompanyUpdateRequest request)
    {
        var userId = GetUserId();
        var company = await _companyRepository.GetByOwnerIdAsync(userId);

        if (company == null)
        {
            return NotFound();
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

        var updatedCompany = await _companyRepository.UpdateAsync(company);

        return Ok(new CompanyResponse(
            updatedCompany.Id,
            updatedCompany.Name,
            updatedCompany.Description,
            updatedCompany.TimeZone,
            updatedCompany.IsActive,
            updatedCompany.OwnerId));
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(userIdClaim!);
    }
}
