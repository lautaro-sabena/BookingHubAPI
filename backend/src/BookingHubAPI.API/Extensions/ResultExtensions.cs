using BookingHubAPI.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace BookingHubAPI.API.Extensions;

/// <summary>Maps application <see cref="Result"/> outcomes to HTTP responses.</summary>
public static class ResultExtensions
{
    /// <summary>
    /// Returns <paramref name="onSuccess"/> applied to the value of a successful result, or the
    /// HTTP response for its error (see <see cref="ToFailureResult"/>).
    /// </summary>
    public static ActionResult ToActionResult<T>(
        this ControllerBase controller, Result<T> result, Func<T, ActionResult> onSuccess) =>
        result.IsSuccess ? onSuccess(result.Value) : controller.ToFailureResult(result.Error!);

    /// <summary>
    /// Validation, conflict and unauthorized errors carry their message as <c>{ "error": message }</c> (400 / 409 / 401).
    /// Not-found and forbidden errors are empty 404 / 403 responses so nothing about the
    /// resource is disclosed.
    /// </summary>
    public static ActionResult ToFailureResult(this ControllerBase controller, Error error) =>
        error.Kind switch
        {
            ErrorKind.Validation => controller.BadRequest(new { error = error.Message }),
            ErrorKind.Conflict => controller.Conflict(new { error = error.Message }),
            ErrorKind.Unauthorized => controller.Unauthorized(new { error = error.Message }),
            ErrorKind.NotFound => controller.NotFound(),
            ErrorKind.Forbidden => controller.Forbid(),
            _ => throw new ArgumentOutOfRangeException(nameof(error), error.Kind, "Unmapped error kind")
        };
}
