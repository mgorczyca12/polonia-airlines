using flight_service.Domain.Entities;
using flight_service.Infrastructure.Persistence;

namespace flight_service.Application.Features.FlightFeatures.Queries;

public interface IFlightQueries
{
    Task<IEnumerable<Flight>> GetFlights(
        string departureAirport, 
        string arrivalAirport, 
        DateTime departureDate, 
        int? page, 
        int? pageSize, 
        CancellationToken cancellationToken);
        
    Task<Flight> GetFlight(
        string flightId, 
        CancellationToken cancellationToken);
}