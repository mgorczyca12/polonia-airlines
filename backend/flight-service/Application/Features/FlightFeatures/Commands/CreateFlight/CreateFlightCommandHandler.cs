using flight_service.Application.Abstractions;
using flight_service.Domain.Entities;
using DispatchR.Abstractions.Send;
using FluentResults;

namespace flight_service.Application.Features.FlightFeatures.Commands.CreateFlight;

public sealed class CreateFlightCommandHandler : IRequestHandler<CreateFlightCommand, ValueTask<Result>>
{
    public async ValueTask<Result> Handle(
        CreateFlightCommand request,
        CancellationToken cancellationToken)
    {
        // Implement the command handling logic here
        throw new NotImplementedException();
    }
}