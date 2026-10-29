using System.Diagnostics;
using System.Text.Json.Serialization;
using flight_service.Application.Contracts.Requests;
using Shared.Api.Middleware;

namespace flight_service.Api.Extensions;

public static class DependencyInjection
{
    public static WebApplicationBuilder AddApi(this WebApplicationBuilder builder)
    {
        builder.Services.AddControllers().AddJsonOptions(options =>
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter<SeatMapRowKind>()));

        builder.Services.AddAuthorization();
        builder.Services.AddOpenApi();
        builder.Services.AddExceptionHandler<ApiExceptionHandler>();

        builder.Services.AddProblemDetails(options =>
            options.CustomizeProblemDetails = context =>
                context.ProblemDetails.Extensions["traceId"] =
                    Activity.Current?.Id ?? context.HttpContext.TraceIdentifier);

        return builder;
    }
}
