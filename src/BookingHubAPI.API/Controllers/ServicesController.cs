using AutoMapper;
using BookingHubAPI.Application.DTOs;
using BookingHubAPI.Domain.Entities;
using BookingHubAPI.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BookingHubAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ServicesController : ControllerBase
{
    private readonly IServiceRepository _serviceRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public ServicesController(
        IServiceRepository serviceRepository,
        ICompanyRepository companyRepository,
        IUserRepository userRepository,
        IMapper mapper)
    {
        _serviceRepository = serviceRepository;
        _companyRepository = companyRepository;
        _userRepository = userRepository;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ServiceResponse>>> GetServices(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        var userId = GetUserId();
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null)
        {
            return NotFound();
        }

        Guid companyId;
        
        if (user.Role == UserRole.Owner && user.CompanyId.HasValue)
        {
            companyId = user.CompanyId.Value;
        }
        else
        {
            return Forbid();
        }

        var services = await _serviceRepository.GetByCompanyIdAsync(companyId, page, pageSize, search);
        var totalCount = await _serviceRepository.GetCountByCompanyIdAsync(companyId, search);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var serviceResponses = services.Select(s => new ServiceResponse(
            s.Id, s.Name, s.Description, s.DurationMinutes, s.Price, s.IsActive, s.CompanyId)).ToList();

        return Ok(new PagedResult<ServiceResponse>(serviceResponses, totalCount, page, pageSize, totalPages));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ServiceResponse>> GetService(Guid id)
    {
        var userId = GetUserId();
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null)
        {
            return NotFound();
        }

        var service = await _serviceRepository.GetByIdAsync(id);

        if (service == null)
        {
            return NotFound();
        }

        if (user.Role == UserRole.Owner && user.CompanyId.HasValue && service.CompanyId == user.CompanyId.Value)
        {
            return Ok(new ServiceResponse(
                service.Id,
                service.Name,
                service.Description,
                service.DurationMinutes,
                service.Price,
                service.IsActive,
                service.CompanyId));
        }

        if (service.IsActive)
        {
            return Ok(new ServiceResponse(
                service.Id,
                service.Name,
                service.Description,
                service.DurationMinutes,
                service.Price,
                service.IsActive,
                service.CompanyId));
        }

        return Forbid();
    }

    [HttpPost]
    public async Task<ActionResult<ServiceResponse>> CreateService([FromBody] ServiceRequest request)
    {
        var userId = GetUserId();
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null || user.Role != UserRole.Owner || !user.CompanyId.HasValue)
        {
            return Forbid();
        }

        var service = new Domain.Entities.Service
        {
            Name = request.Name,
            Description = request.Description,
            DurationMinutes = request.DurationMinutes,
            Price = request.Price,
            CompanyId = user.CompanyId.Value
        };

        var createdService = await _serviceRepository.CreateAsync(service);

        return CreatedAtAction(nameof(GetServices), new ServiceResponse(
            createdService.Id,
            createdService.Name,
            createdService.Description,
            createdService.DurationMinutes,
            createdService.Price,
            createdService.IsActive,
            createdService.CompanyId));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ServiceResponse>> UpdateService(Guid id, [FromBody] ServiceUpdateRequest request)
    {
        var userId = GetUserId();
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null || user.Role != UserRole.Owner || !user.CompanyId.HasValue)
        {
            return Forbid();
        }

        var service = await _serviceRepository.GetByIdAsync(id);

        if (service == null || service.CompanyId != user.CompanyId.Value)
        {
            return NotFound();
        }

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

        var updatedService = await _serviceRepository.UpdateAsync(service);

        return Ok(new ServiceResponse(
            updatedService.Id,
            updatedService.Name,
            updatedService.Description,
            updatedService.DurationMinutes,
            updatedService.Price,
            updatedService.IsActive,
            updatedService.CompanyId));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteService(Guid id)
    {
        var userId = GetUserId();
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null || user.Role != UserRole.Owner || !user.CompanyId.HasValue)
        {
            return Forbid();
        }

        var service = await _serviceRepository.GetByIdAsync(id);

        if (service == null || service.CompanyId != user.CompanyId.Value)
        {
            return NotFound();
        }

        service.IsActive = false;
        await _serviceRepository.UpdateAsync(service);

        return NoContent();
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(userIdClaim!);
    }
}
