using DispatchR.Abstractions.Send;
using flight_service.Application.Abstractions;
using flight_service.Application.Features.ConfigurationFeatures.Errors;
using FluentResults;

namespace flight_service.Application.Features.ConfigurationFeatures.Commands.RemoveSeatMapRow;

public sealed class RemoveSeatMapRowCommandHandler(IConfigurationRepository configurations)
    : IRequestHandler<RemoveSeatMapRowCommand, ValueTask<Result>>
{
    public async ValueTask<Result> Handle(RemoveSeatMapRowCommand request, CancellationToken cancellationToken)
    {
        var configuration = await configurations.GetWithSeatMapAsync(request.ConfigurationId, cancellationToken);
        if (configuration is null)
        {
            return Result.Fail(ConfigurationError.Missing("configuration"));
        }

        var row = configuration.SeatMap.SeatMapRows.SingleOrDefault(value => value.Number == request.RowNumber);
        if (row is null)
        {
            return Result.Fail(ConfigurationError.Missing("seat_map_row"));
        }

        configuration.SeatMap.RemoveRow(row);
        return Result.Ok();
    }
}
