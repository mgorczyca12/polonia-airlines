using Shared.Domain.Entities;

namespace booking_service.Domain.Entities
{
    public class Baggage : Entity<int>
    {
        public int PassengerId { get; private set; }
        public Passenger Passenger { get; private set; } = null!;
        public string Name { get; private set; } = null!;
        public decimal WeightKg { get; private set; }

        private Baggage() { }

        internal static Baggage Create(Passenger passenger, string name, decimal weightKg)
        {
            ArgumentNullException.ThrowIfNull(passenger);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(weightKg);
            return new Baggage
            {
                PassengerId = passenger.Id,
                Passenger = passenger,
                Name = name.Trim(),
                WeightKg = weightKg
            };
        }
    }
}