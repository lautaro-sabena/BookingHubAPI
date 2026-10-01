using BookingHubAPI.Application.DTOs;
using BookingHubAPI.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BookingHubAPI.API.Extensions;

namespace BookingHubAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CompaniesController : ControllerBase
{
    private readonly ICompanyService _companyService;

    public CompaniesController(ICompanyService companyService)
    {
        _companyService = companyService;
    }

    [HttpGet("me")]
    public async Task<ActionResult<CompanyResponse>> GetMyCompany()
    {
        var result = await _companyService.GetMyCompanyAsync(User.GetUserId());
        return this.ToActionResult(result, Ok);
    }

    [HttpPost]
    public async Task<ActionResult<CompanyResponse>> CreateCompany([FromBody] CompanyRequest request)
    {
        var result = await _companyService.CreateCompanyAsync(User.GetUserId(), request);
        return this.ToActionResult(result, company => CreatedAtAction(nameof(GetMyCompany), company));
    }

    [HttpPut("me")]
    public async Task<ActionResult<CompanyResponse>> UpdateMyCompany([FromBody] CompanyUpdateRequest request)
    {
        var result = await _companyService.UpdateMyCompanyAsync(User.GetUserId(), request);
        return this.ToActionResult(result, Ok);
    }
}
