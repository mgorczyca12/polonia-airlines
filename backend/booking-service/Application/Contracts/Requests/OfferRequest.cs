namespace booking_service.Application.Contracts.Requests;

/// <summary>A fare product chosen for a flight, for every passenger or for one passenger index.</summary>
public sealed record OfferSelection(string FlightId, Guid ProductId, int? PassengerIndex = null);

/// <summary>Requests a server-priced offer for the selected products in one currency.</summary>
public sealed record OfferRequest(string Currency, int PassengerCount, OfferSelection[] Selections);
