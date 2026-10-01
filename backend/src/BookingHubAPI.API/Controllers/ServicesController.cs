using BookingHubAPI.Application.DTOs;
using BookingHubAPI.Application.Services;
using BookingHubAPI.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BookingHubAPI.API.Extensions;

namespace BookingHubAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ServicesController : ControllerBase
{
    private readonly IServiceCatalogService _serviceCatalog;

    public ServicesController(IServiceCatalogService serviceCatalog)
    {
        _serviceCatalog = serviceCatalog;
    }

    [HttpGet]
    [Authorize(Roles = RoleNames.Owner)]
    public async Task<ActionResult<PagedResult<ServiceResponse>>> GetServices(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        var result = await _serviceCatalog.GetOwnServicesAsync(User.GetUserId(), page, pageSize, search);
        return this.ToActionResult(result, Ok);
    }

    [HttpGet("all")]
    public async Task<ActionResult<PagedResult<ServiceResponse>>> GetAllServices(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        return Ok(await _serviceCatalog.GetPublicServicesAsync(page, pageSize));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ServiceResponse>> GetService(Guid id)
    {
        var result = await _serviceCatalog.GetServiceAsync(User.GetUserId(), id);
        return this.ToActionResult(result, Ok);
    }

    [HttpPost]
    public async Task<ActionResult<ServiceResponse>> CreateService([FromBody] ServiceRequest request)
    {
        var result = await _serviceCatalog.CreateServiceAsync(User.GetUserId(), request);
        return this.ToActionResult(result, service => CreatedAtAction(nameof(GetServices), service));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ServiceResponse>> UpdateService(Guid id, [FromBody] ServiceUpdateRequest request)
    {
        var result = await _serviceCatalog.UpdateServiceAsync(User.GetUserId(), id, request);
        return this.ToActionResult(result, Ok);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteService(Guid id)
    {
        var result = await _serviceCatalog.DeleteServiceAsync(User.GetUserId(), id);
        return result.IsSuccess ? NoContent() : this.ToFailureResult(result.Error!);
    }
}
