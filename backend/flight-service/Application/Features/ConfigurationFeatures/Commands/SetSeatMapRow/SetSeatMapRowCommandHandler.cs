using DispatchR.Abstractions.Send;
using flight_service.Application.Abstractions;
using flight_service.Application.Features.ConfigurationFeatures.Errors;
using flight_service.Domain.Entities;
using flight_service.Domain.ValueObjects;
using FluentResults;

namespace flight_service.Application.Features.ConfigurationFeatures.Commands.SetSeatMapRow;

public sealed class SetSeatMapRowCommandHandler(IConfigurationRepository configurations)
    : IRequestHandler<SetSeatMapRowCommand, ValueTask<Result>>
{
    public async ValueTask<Result> Handle(SetSeatMapRowCommand request, CancellationToken cancellationToken)
    {
        var configuration = await configurations.GetWithSeatMapAsync(request.ConfigurationId, cancellationToken);
        if (configuration is null)
        {
            return Result.Fail(ConfigurationError.Missing("configuration"));
        }

        var cabin = await configurations.GetCabinAsync(request.Row.CabinId, cancellationToken);
        if (cabin is null)
        {
            return Result.Fail(ConfigurationError.Missing("cabin"));
        }

        var type = SeatMapRowType.FromValue((int)request.Row.Type);
        var row = configuration.SeatMap.SeatMapRows.SingleOrDefault(value => value.Number == request.RowNumber);
        if (row is null)
        {
            row = SeatMapRow.Create(configuration.SeatMap, cabin, type, request.RowNumber);
            configuration.SeatMap.AddRow(row);
        }
        else
        {
            row.Update(cabin, type);
        }

        var desired = request.Row.Positions.ToDictionary(position => position.Column, StringComparer.Ordinal);
        foreach (var position in row.SeatMapPositions.ToArray())
        {
            if (!desired.ContainsKey(position.Column))
            {
                row.RemovePosition(position);
            }
        }

        foreach (var input in request.Row.Positions)
        {
            var position = row.SeatMapPositions.SingleOrDefault(value =>
                string.Equals(value.Column, input.Column, StringComparison.Ordinal));
            if (position is null)
            {
                row.AddPosition(SeatMapPosition.Create(input.Column, input.AisleGroup, input.IsSeat));
            }
            else
            {
                position.Update(input.AisleGroup, input.IsSeat);
            }
        }

        return Result.Ok();
    }
}
