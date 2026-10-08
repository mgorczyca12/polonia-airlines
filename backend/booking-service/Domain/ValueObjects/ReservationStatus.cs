namespace booking_service.Domain.ValueObjects
{
    public sealed record ReservationStatus
    {
        public static readonly ReservationStatus Created = new ReservationStatus("Created");
        public static readonly ReservationStatus Reserved = new ReservationStatus("Reserved");
        public static readonly ReservationStatus Cancelled = new ReservationStatus("Cancelled");

        public string Status { get; }

        public static ReservationStatus From(string status) => status switch
        {
            "Created" => Created,
            "Reserved" => Reserved,
            "Cancelled" => Cancelled,
            _ => throw new ArgumentException("Unknown reservation status.", nameof(status))
        };

        private ReservationStatus(string status)
        {
            Status = status;
        }
    }
}