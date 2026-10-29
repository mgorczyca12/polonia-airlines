using booking_service.Infrastructure.Persistence;
using MassTransit;

namespace booking_service.Infrastructure.Integrations.Sagas;

public static class ReservationWorkflowRegistration
{
    public static void AddReservationWorkflow(this IBusRegistrationConfigurator bus)
    {
        bus.AddSagaStateMachine<ReservationStateMachine, ReservationSagaState>()
            .EntityFrameworkRepository(repository =>
            {
                repository.ConcurrencyMode = ConcurrencyMode.Pessimistic;
                repository.ExistingDbContext<BookingDbContext>();
                repository.UsePostgres();
            });
    }
}
