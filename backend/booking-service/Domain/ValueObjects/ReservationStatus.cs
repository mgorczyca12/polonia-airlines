namespace booking_service.Domain.ValueObjects
{
    public sealed record ReservationStatus
    {
        public static readonly ReservationStatus Created = new ReservationStatus("Created");
        public static readonly ReservationStatus Reserved = new ReservationStatus("Reserved");
        public static readonly ReservationStatus Cancelled = new ReservationStatus("Cancelled");

        public string Status { get; }

        private ReservationStatus(string status)
        {
            Status = status;
        }
    }
}