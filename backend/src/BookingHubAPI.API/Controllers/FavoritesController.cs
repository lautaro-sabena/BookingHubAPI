using BookingHubAPI.Application.DTOs;
using BookingHubAPI.Application.Services;
using BookingHubAPI.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BookingHubAPI.API.Extensions;

namespace BookingHubAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = RoleNames.Customer)]
public class FavoritesController : ControllerBase
{
    private readonly IFavoriteService _favoriteService;

    public FavoritesController(IFavoriteService favoriteService)
    {
        _favoriteService = favoriteService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<FavoriteDto>>> GetFavorites()
    {
        return Ok(await _favoriteService.GetFavoritesAsync(User.GetUserId()));
    }

    [HttpPost("{serviceId}")]
    public async Task<ActionResult<FavoriteDto>> AddFavorite(Guid serviceId)
    {
        var result = await _favoriteService.AddFavoriteAsync(User.GetUserId(), serviceId);
        return this.ToActionResult(result, Ok);
    }

    [HttpDelete("{serviceId}")]
    public async Task<IActionResult> RemoveFavorite(Guid serviceId)
    {
        var result = await _favoriteService.RemoveFavoriteAsync(User.GetUserId(), serviceId);
        return result.IsSuccess ? NoContent() : this.ToFailureResult(result.Error!);
    }

    [HttpGet("{serviceId}/check")]
    public async Task<ActionResult<bool>> CheckFavorite(Guid serviceId)
    {
        return Ok(await _favoriteService.IsFavoriteAsync(User.GetUserId(), serviceId));
    }
}
