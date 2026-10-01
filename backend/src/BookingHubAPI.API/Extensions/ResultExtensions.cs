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
    /// Every error becomes an RFC 7807 <c>application/problem+json</c> response whose
    /// <c>detail</c> is the error message (400 / 401 / 403 / 404 / 409). The standard
    /// <c>title</c>, <c>type</c>, <c>status</c> and <c>traceId</c> come from the configured
    /// <see cref="ProblemDetailsFactory"/>.
    /// </summary>
    public static ActionResult ToFailureResult(this ControllerBase controller, Error error) =>
        controller.Problem(detail: error.Message, statusCode: error.Kind switch
        {
            ErrorKind.Validation => StatusCodes.Status400BadRequest,
            ErrorKind.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorKind.Forbidden => StatusCodes.Status403Forbidden,
            ErrorKind.NotFound => StatusCodes.Status404NotFound,
            ErrorKind.Conflict => StatusCodes.Status409Conflict,
            _ => throw new ArgumentOutOfRangeException(nameof(error), error.Kind, "Unmapped error kind")
        });
}
