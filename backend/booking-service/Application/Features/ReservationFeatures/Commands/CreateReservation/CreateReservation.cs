using FluentResults;
using Shared.Application.Commands;
using booking_service.Application.Contracts;

namespace booking_service.Application.Features.ReservationFeatures.Commands.CreateReservation;

public sealed record CreateReservationCommand(string GuestToken, ReservationRequest Request)
    : ICommand<CreateReservationCommand, Result<ReservationResponse>>;
