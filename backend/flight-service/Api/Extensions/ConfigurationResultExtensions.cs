using flight_service.Application.Features.ConfigurationFeatures.Errors;
using FluentResults;
using Microsoft.AspNetCore.Mvc;

namespace flight_service.Api.Extensions;

public static class ConfigurationResultExtensions
{
    public static ObjectResult ConfigurationFailure(this ControllerBase controller, IResultBase result)
    {
        if (result.Errors.Count != 1 || result.Errors[0] is not ConfigurationError error)
        {
            throw new InvalidOperationException("The configuration operation returned an unrecognized failure.");
        }

        var (status, title) = error.Kind switch
        {
            ConfigurationErrorKind.NotFound => (StatusCodes.Status404NotFound, "Resource not found"),
            ConfigurationErrorKind.Conflict => (StatusCodes.Status409Conflict, "Operation unavailable"),
            _ => throw new InvalidOperationException("The configuration operation returned an unrecognized error kind.")
        };

        return controller.Problem(
            statusCode: status,
            title: title,
            detail: error.Message,
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
    }
}
