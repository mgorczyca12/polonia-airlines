using flight_service.Application.Features.FlightFeatures.Queries;
using flight_service.Infrastructure.Persistence;
using flight_service.Domain.Entities;

namespace flight_service.Infrastructure.Persistence.Queries;

public class FlightQueries(FlightServiceDbContext context) : IFlightQueries
{
    public async Task<Flight> GetFlight(string flightId, CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    public async Task<IEnumerable<Flight>> GetFlights(string departureAirport, string arrivalAirport, DateTime departureDate, int? page, int? pageSize, CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }
}