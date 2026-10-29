using booking_service.Application.Contracts.Requests;
using FluentResults;
using Shared.Application.Commands;

namespace booking_service.Application.Features.ReservationFeatures.Commands.CreateReservation;

/// <summary>Returns the new, or previously created for the same request ID, reservation's ID.</summary>
public sealed record CreateReservationCommand(string GuestToken, ReservationRequest Request)
    : ITransactionalCommand<CreateReservationCommand, Result<int>>;
