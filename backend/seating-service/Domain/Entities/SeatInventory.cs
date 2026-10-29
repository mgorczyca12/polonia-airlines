using seating_service.Domain.ValueObjects;
using Shared.Domain.Entities;

namespace seating_service.Domain.Entities;

public class SeatInventory : AggregateRoot<int>
{
    private readonly List<SeatAssignment> assignments = new();

    public string FlightId { get; private set; } = null!;
    public string CabinId { get; private set; } = null!;
    public int TotalSeats { get; private set; }
    public int OverbookingAllowance { get; private set; }
    public int ReservedSeats { get; private set; }
    public int BookingLimit => TotalSeats + OverbookingAllowance;
    public int RemainingBookingCapacity => BookingLimit - ReservedSeats;
    public int OverbookedSeats => Math.Max(0, ReservedSeats - TotalSeats);
    public IReadOnlyCollection<SeatAssignment> Assignments => assignments.AsReadOnly();

    private SeatInventory()
    {
    }

    public static SeatInventory Create(
        string flightId,
        string cabinId,
        int totalSeats,
        int overbookingAllowance = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(flightId);
        ArgumentException.ThrowIfNullOrWhiteSpace(cabinId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(totalSeats);
        ArgumentOutOfRangeException.ThrowIfNegative(overbookingAllowance);

        if (overbookingAllowance > int.MaxValue - totalSeats)
        {
            throw new ArgumentOutOfRangeException(
                nameof(overbookingAllowance),
                "Booking limit exceeds the supported capacity.");
        }

        return new SeatInventory
        {
            FlightId = flightId,
            CabinId = cabinId,
            TotalSeats = totalSeats,
            OverbookingAllowance = overbookingAllowance
        };
    }

    public SeatAssignment ConfirmAllocation(string reservationId, string passengerId, string? seatId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reservationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(passengerId);

        if (assignments.Any(assignment => assignment.PassengerId == passengerId))
        {
            throw new InvalidOperationException("Passenger already has an allocation in this inventory.");
        }

        if (RemainingBookingCapacity <= 0)
        {
            throw new InvalidOperationException("The cabin booking limit has been reached.");
        }

        EnsureSeatAvailable(seatId);

        var assignment = SeatAssignment.Create(this, reservationId, passengerId, seatId);
        assignments.Add(assignment);
        ReservedSeats++;
        return assignment;
    }

    public void AssignSeat(SeatAssignment assignment, string seatId)
    {
        EnsureOwned(assignment);
        ArgumentException.ThrowIfNullOrWhiteSpace(seatId);
        EnsureSeatAvailable(seatId, assignment);
        assignment.ChangeSeat(seatId);
    }

    public void ClearSeat(SeatAssignment assignment)
    {
        EnsureOwned(assignment);
        assignment.ChangeSeat(null);
    }

    public void ReleaseAllocation(SeatAssignment assignment)
    {
        EnsureOwned(assignment);
        if (ReservedSeats <= 0)
        {
            throw new InvalidOperationException("Confirmed allocation count is inconsistent.");
        }

        assignments.Remove(assignment);
        ReservedSeats--;
    }

    public bool CanConfirm(IEnumerable<HeldSelection> selections)
    {
        ArgumentNullException.ThrowIfNull(selections);

        var requested = selections
            .Where(selection => selection.FlightId == FlightId && selection.CabinId == CabinId)
            .ToArray();

        var requestedSeats = requested.Count(selection => selection.SeatId is not null);
        var assignedSeats = assignments.Count(assignment => assignment.SeatId is not null);
        var conflicts = requested.Any(selection => assignments.Any(assignment =>
            assignment.PassengerId == selection.PassengerId ||
            (selection.SeatId is not null && assignment.SeatId == selection.SeatId)));

        return assignments.Count + requested.Length <= BookingLimit &&
            assignedSeats + requestedSeats <= TotalSeats &&
            !conflicts;
    }

    public void EnsureConsistent()
    {
        if (ReservedSeats != assignments.Count)
        {
            throw new InvalidOperationException("Confirmed inventory count is inconsistent.");
        }
    }

    private void EnsureOwned(SeatAssignment assignment)
    {
        ArgumentNullException.ThrowIfNull(assignment);

        if (!assignments.Any(existing => ReferenceEquals(existing, assignment)))
        {
            throw new InvalidOperationException("Allocation does not belong to this inventory.");
        }
    }

    private void EnsureSeatAvailable(string? seatId, SeatAssignment? current = null)
    {
        if (seatId is null)
        {
            return;
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(seatId);

        if (assignments.Any(assignment => !ReferenceEquals(assignment, current) && assignment.SeatId == seatId))
        {
            throw new InvalidOperationException("Seat is already assigned in this inventory.");
        }

        if ((current is null || current.SeatId is null) &&
            assignments.Count(assignment => assignment.SeatId is not null) >= TotalSeats)
        {
            throw new InvalidOperationException("All physical seats in this cabin are assigned.");
        }
    }
}
