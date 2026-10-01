using BookingHubAPI.Application.Common;
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
        return result.IsSuccess ? Ok(result.Value) : ToPlainTextFailure(result.Error!);
    }

    [HttpDelete("{serviceId}")]
    public async Task<IActionResult> RemoveFavorite(Guid serviceId)
    {
        var result = await _favoriteService.RemoveFavoriteAsync(User.GetUserId(), serviceId);
        return result.IsSuccess ? NoContent() : ToPlainTextFailure(result.Error!);
    }

    [HttpGet("{serviceId}/check")]
    public async Task<ActionResult<bool>> CheckFavorite(Guid serviceId)
    {
        return Ok(await _favoriteService.IsFavoriteAsync(User.GetUserId(), serviceId));
    }

    /// <summary>
    /// This API has always answered favorites failures with the bare message as the body
    /// (not the <c>{ "error": ... }</c> object used elsewhere); clients may depend on it.
    /// </summary>
    private ActionResult ToPlainTextFailure(Error error) => error.Kind switch
    {
        ErrorKind.NotFound => NotFound(error.Message),
        ErrorKind.Validation => BadRequest(error.Message),
        _ => this.ToFailureResult(error)
    };
}
