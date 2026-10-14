using Shared.Domain.Entities;

namespace booking_service.Domain.Entities
{
    public class Baggage : Entity<int>
    {
        public int PassengerId { get; private set; }
        public Passenger Passenger { get; private set; } = null!;
        public int PassengerFareId { get; private set; }
        public PassengerFare PassengerFare { get; private set; } = null!;
        public string Name { get; private set; } = null!;
        public decimal WeightKg { get; private set; }
        public decimal Charge { get; private set; }

        private Baggage() { }

        internal static Baggage Create(Passenger passenger, PassengerFare fare, string name, decimal weightKg,
            decimal charge)
        {
            ArgumentNullException.ThrowIfNull(passenger);
            ArgumentNullException.ThrowIfNull(fare);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(weightKg);
            ArgumentOutOfRangeException.ThrowIfNegative(charge);
            return new Baggage
            {
                PassengerId = passenger.Id,
                Passenger = passenger,
                PassengerFareId = fare.Id,
                PassengerFare = fare,
                Name = name.Trim(),
                WeightKg = weightKg,
                Charge = charge
            };
        }
    }
}