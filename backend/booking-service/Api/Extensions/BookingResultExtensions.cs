using booking_service.Application.Errors;
using FluentResults;
using Microsoft.AspNetCore.Mvc;

namespace booking_service.Api.Extensions;

public static class BookingResultExtensions
{
    public static ObjectResult BookingFailure(this ControllerBase controller, IResultBase result)
    {
        if (result.Errors.Count != 1 || result.Errors[0] is not BookingError error)
        {
            throw new InvalidOperationException("The booking operation returned an unrecognized failure.");
        }

        var (status, title) = error.Kind switch
        {
            BookingErrorKind.NotFound => (StatusCodes.Status404NotFound, "Resource not found"),
            BookingErrorKind.Invalid => (StatusCodes.Status400BadRequest, "Request cannot be fulfilled"),
            BookingErrorKind.Conflict => (StatusCodes.Status409Conflict, "Operation unavailable"),
            BookingErrorKind.Unavailable => (StatusCodes.Status503ServiceUnavailable, "Service temporarily unavailable"),
            _ => throw new InvalidOperationException("The booking operation returned an unrecognized error kind.")
        };

        return controller.Problem(
            statusCode: status,
            title: title,
            detail: error.Message,
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
    }
}
