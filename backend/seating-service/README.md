# Seating service domain

SeatInventory represents one flight/cabin inventory bucket. FlightId and CabinId
are opaque string references owned by flight-service; the integration boundary
supplies their canonical representation. Non-blank IDs are preserved without
parsing, trimming, or case changes. Inventory's own identity remains an integer.
TotalSeats is physical sellable capacity; OverbookingAllowance is additional
booking capacity and defaults to zero. BookingLimit is their sum.
RemainingBookingCapacity subtracts confirmed cabin allocations from that
limit. OverbookedSeats is the confirmed excess over physical capacity, not the
configured allowance. Availability is not clamped, so a future capacity reduction
can expose a shortfall rather than hide it.

Physical capacity is populated from the assigned aircraft configuration's usable
seats per cabin. Fare products share cabin capacity rather than having independent
physical-seat pools. Future persistence should enforce uniqueness on FlightId/CabinId
using comparison semantics consistent with the owning service's contract.

Temporary physical-seat holds belong in Redis, not an inventory HeldSeats column.
The database will store confirmed cabin allocations and optional physical-seat
assignments. Overbooking requires some confirmed allocations without assigned
seats and never permits duplicate physical-seat assignments.

SeatAssignment represents a confirmed passenger/cabin allocation with an optional
physical SeatId. It belongs to its inventory through a local integer foreign key
and parent navigation. ReservationId, PassengerId, and SeatId are opaque external
references; flight and cabin are determined by the parent inventory.

Use ConfirmAllocation, AssignSeat, ClearSeat, and ReleaseAllocation on the inventory.
Its read-only Assignments collection cannot be directly modified. Confirmation
increments ReservedSeats; release decrements it. Changing or clearing a physical
seat does not change booked capacity. Duplicate passengers, duplicate seat IDs,
physical assignments beyond TotalSeats, and bookings beyond BookingLimit are rejected.
Release removes the allocation from the active inventory; durable audit/history
retention is a future persistence concern.

The application must verify seat membership/eligibility in the aircraft layout,
validate Redis hold ownership, and coordinate confirmation and cleanup. Domain
checks require loading all assignments and do not protect separate concurrent
requests by themselves. Future persistence must enforce unique passenger allocations
per flight, unique non-null seat assignments per flight, map the assignments backing
field, and atomically protect the booking count. Redis, persistence, hold expiry,
capacity changes, and analytics-driven policy updates are not implemented yet.
