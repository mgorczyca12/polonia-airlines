using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace flight_service.Api.Middleware;

public sealed class ApiExceptionHandler(
    ILogger<ApiExceptionHandler> logger,
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled exception processing {Method} {Path}",
            httpContext.Request.Method, httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        var context = new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
                Detail = "Unable to complete the request. Please try again later.",
                Instance = httpContext.Request.Path
            }
        };
        if (!await problemDetailsService.TryWriteAsync(context))
        {
            context.ProblemDetails.Extensions["traceId"] =
                Activity.Current?.Id ?? httpContext.TraceIdentifier;
            await httpContext.Response.WriteAsJsonAsync(
                context.ProblemDetails,
                options: null,
                contentType: "application/problem+json",
                cancellationToken: cancellationToken);
        }
        return true;
    }
}
