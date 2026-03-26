using AutoMapper;
using BookingHubAPI.Application.DTOs;
using BookingHubAPI.Domain.Entities;
using BookingHubAPI.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BookingHubAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Customer")]
public class FavoritesController : ControllerBase
{
    private readonly IFavoriteRepository _favoriteRepository;
    private readonly IServiceRepository _serviceRepository;
    private readonly IMapper _mapper;

    public FavoritesController(
        IFavoriteRepository favoriteRepository,
        IServiceRepository serviceRepository,
        IMapper mapper)
    {
        _favoriteRepository = favoriteRepository;
        _serviceRepository = serviceRepository;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<FavoriteDto>>> GetFavorites()
    {
        var userId = GetUserId();
        var favorites = await _favoriteRepository.GetByCustomerIdAsync(userId);
        var favoriteDtos = _mapper.Map<IEnumerable<FavoriteDto>>(favorites);
        return Ok(favoriteDtos);
    }

    [HttpPost("{serviceId}")]
    public async Task<ActionResult<FavoriteDto>> AddFavorite(Guid serviceId)
    {
        var userId = GetUserId();

        // Check if service exists
        var service = await _serviceRepository.GetByIdWithCompanyAsync(serviceId);
        if (service == null)
        {
            return NotFound("Service not found");
        }

        // Check if already favorited
        var existing = await _favoriteRepository.GetByCustomerAndServiceAsync(userId, serviceId);
        if (existing != null)
        {
            return BadRequest("Service already in favorites");
        }

        var favorite = new Favorite
        {
            CustomerId = userId,
            ServiceId = serviceId
        };

        var created = await _favoriteRepository.AddAsync(favorite);
        
        var favoriteDto = new FavoriteDto
        {
            Id = created.Id,
            ServiceId = service.Id,
            ServiceName = service.Name,
            ServiceDescription = service.Description,
            DurationMinutes = service.DurationMinutes,
            Price = service.Price,
            CompanyId = service.CompanyId,
            CompanyName = service.Company.Name
        };

        return Ok(favoriteDto);
    }

    [HttpDelete("{serviceId}")]
    public async Task<IActionResult> RemoveFavorite(Guid serviceId)
    {
        var userId = GetUserId();
        var removed = await _favoriteRepository.RemoveAsync(userId, serviceId);
        
        if (!removed)
        {
            return NotFound("Favorite not found");
        }

        return NoContent();
    }

    [HttpGet("{serviceId}/check")]
    public async Task<ActionResult<bool>> CheckFavorite(Guid serviceId)
    {
        var userId = GetUserId();
        var exists = await _favoriteRepository.ExistsAsync(userId, serviceId);
        return Ok(exists);
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(userIdClaim!);
    }
}