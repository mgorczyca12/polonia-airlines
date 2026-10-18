using flight_service.Application.Contracts.Requests;
using FluentResults;
using Shared.Application.Commands;

namespace flight_service.Application.Features.ConfigurationFeatures.Commands.SetSeatMapRow;

public sealed record SetSeatMapRowCommand(int ConfigurationId, int RowNumber, SeatMapRowRequest Row)
    : ITransactionalCommand<SetSeatMapRowCommand, Result>;
