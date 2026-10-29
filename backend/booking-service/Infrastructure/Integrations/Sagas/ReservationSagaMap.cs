using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace booking_service.Infrastructure.Integrations.Sagas;

public sealed class ReservationSagaMap : SagaClassMap<ReservationSagaState>
{
    protected override void Configure(
        EntityTypeBuilder<ReservationSagaState> entity,
        ModelBuilder model)
    {
        entity.ToTable("ReservationSagas");
        entity.Property(state => state.CurrentState).HasMaxLength(64).IsRequired();
        entity.Property(state => state.ReservationId).HasMaxLength(32).IsRequired();
        entity.Property(state => state.FailureCode).HasMaxLength(128);
        var utcTimestamp = new ValueConverter<DateTimeOffset, DateTime>(
            value => value.UtcDateTime,
            value => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)));
        entity.Property(state => state.HoldUntil).HasConversion(utcTimestamp);
        entity.Property(state => state.NextCheckAt).HasConversion(utcTimestamp);
        entity.Property(state => state.CompensationStartedAt).HasConversion(utcTimestamp);
        entity.HasIndex(state => state.NextCheckAt);
    }
}
