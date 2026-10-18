using FluentResults;
using Shared.Application.Commands;

namespace flight_service.Application.Features.ConfigurationFeatures.Commands.RemoveSeatMapRow;

public sealed record RemoveSeatMapRowCommand(int ConfigurationId, int RowNumber)
    : ITransactionalCommand<RemoveSeatMapRowCommand, Result>;
