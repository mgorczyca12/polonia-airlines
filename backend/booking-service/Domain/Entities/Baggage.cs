using Shared.Domain.Entities;

namespace booking_service.Domain.Entities
{
    public class Baggage : Entity<int>
    {
        public string Name { get; private set; } = null!;
        public decimal WeightKg { get; private set; }

        private Baggage() { }

        internal static Baggage Create(string name, decimal weightKg)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(weightKg);
            return new Baggage { Name = name.Trim(), WeightKg = weightKg };
        }
    }
}