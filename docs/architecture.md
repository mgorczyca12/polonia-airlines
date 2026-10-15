# Architecture

[Documentation index](README.md)

## Current implementation

Polonia Airlines is a full-stack airline prototype with three ASP.NET Core
service projects and one customer-facing web application.
The repository aims for DDD, Clean Architecture, and CQRS; implementation maturity
differs by service.

| Area | Implemented | Not wired or not implemented |
| --- | --- | --- |
| Web | Search, fare selection, booking preview from local JSON | Backend API client, persistent reservations, payment processing |
| Flight | Validated query endpoints, DTOs, EF model and query service | Database provider in normal startup; working aircraft queries |
| Booking | Reservation aggregate with passenger fares, baggage and recorded payments | Business endpoints, persistence and command handlers |
| Seating | Inventory and seat-assignment domain rules | Business endpoints, persistence and command handlers |
| Shared | Entity/aggregate bases, command pipelines, publisher abstraction | Host registration of shared pipelines, transport and consumers |

## C4 level 1: system context

The customer relationship represents the implemented browser experience. There
are no implemented payment-provider, identity-provider, or other airline-system
integrations to draw.

```mermaid
C4Context
    title Polonia Airlines - current system context
    Person(customer, "Customer", "Explores flights and confirms a local booking preview")
    System(polonia, "Polonia Airlines", "Web prototype and backend service projects; backend integration is incomplete")
    Rel(customer, polonia, "Searches and reviews sample flight and fare data", "Browser")
```

## C4 level 2: containers

These are code-level runtime boundaries, not a claim about deployed infrastructure.
The SQLite container exists only inside integration tests. The isolated service
containers have no drawn inter-service links because none are implemented.

```mermaid
C4Container
    title Polonia Airlines - implemented containers and test-only storage
    Person(customer, "Customer", "Uses the customer web application")
    Person(apiCaller, "API caller / integration tests", "Calls backend routes directly")
    System_Boundary(polonia, "Polonia Airlines repository") {
        Container(web, "polonia-web", "React, TanStack Start/Router, Mantine", "Local JSON-backed search and booking preview")
        Container(flight, "flight-service", "ASP.NET Core / EF Core", "Flight query API; normal host database provider is unconfigured")
        Container(booking, "booking-service", "ASP.NET Core", "Reservation domain library in a host exposing weather scaffold")
        Container(seating, "seating-service", "ASP.NET Core", "Seat-inventory domain library in a host exposing weather scaffold")
        ContainerDb(testDb, "Flight test database", "SQLite in memory", "Integration-test fixture only; not production storage")
    }
    Rel(customer, web, "Uses search and booking-preview pages", "Browser")
    Rel(apiCaller, flight, "Queries flights", "HTTP")
    Rel(apiCaller, booking, "Calls scaffold weather route", "HTTP")
    Rel(apiCaller, seating, "Calls scaffold weather route", "HTTP")
    Rel(flight, testDb, "Reads and writes when the test fixture supplies the provider", "EF Core / SQLite")
```

## C4 level 3: flight-query components

The application interface and validator are implemented in `Application`, while
the concrete read model is in `Infrastructure`. This is a component view of the
flight service, not an additional deployment boundary.

```mermaid
C4Component
    title Flight service - query components
    Container_Boundary(flight, "flight-service") {
        Component(controller, "FlightController", "ASP.NET Core MVC", "Validates HTTP input and translates results")
        Component(validator, "SearchFlightsRequestValidator", "FluentValidation", "Validates search inputs and pagination")
        Component(queryContract, "IFlightQueries", "Application interface", "Result-based DTO query contract")
        Component(queries, "FlightQueries", "EF Core read model", "Projects no-tracking queries to FlightResponse")
        Component(context, "FlightServiceDbContext", "EF Core", "Entity sets and persistence configuration")
        Component(errors, "ApiExceptionHandler", "ASP.NET Core exception handling", "Logs unexpected errors and writes safe ProblemDetails")
    }
    Rel(controller, validator, "Validates search input")
    Rel(controller, queryContract, "Invokes directly with cancellation")
    Rel(queryContract, queries, "Resolved to implementation by DI")
    Rel(queries, context, "Composes and executes reads")
```

Unexpected exceptions propagate to the registered exception handler; it is not
called directly by the controller. See the [flight sequence](services/flight-service.md#query-sequence).

## Layering and shared building blocks

The intended service organization is `Api`, `Application`, `Domain`, and
`Infrastructure`, defined in [repository instructions](../.github/copilot-instructions.md).
These are folders within each service project, not separate assemblies.

- `Api`: HTTP contracts, controllers, validation invocation and error translation.
- `Application`: use-case contracts, response DTOs, query abstractions and validators.
- `Domain`: entities, aggregate invariants and value objects.
- `Infrastructure`: EF mappings, concrete query services and external-system adapters.
- [Shared](../backend/shared/): reusable entity identities, aggregate domain-event
  collection, DispatchR command and validation/transaction pipelines, and the
  MassTransit-backed integration publisher.

The existing flight application registers its query validator directly. It does
not call `AddSharedApplication`; booking and seating do not wire command pipelines
either. Do not infer active mediator processing from package references.
The shared transaction pipeline requires an `IUnitOfWork` implementation.

Cross-service references use opaque strings: booking references flight IDs, and
seating references flight, cabin, reservation, passenger and optional seat IDs.
They are not cross-service foreign keys or entity navigations.

## Integration and persistence limitations

No concrete integration-event contracts, consumers, broker transport registration,
or implemented service-to-service calls were found. There is therefore no current
cross-service sequence diagram. The MassTransit publisher and domain-event
collection are building blocks, not evidence of a working outbox/inbox flow.

The flight service has EF configurations and relational integration tests, but
normal startup registers `FlightServiceDbContext` without a provider. No migrations
are checked in. Booking and seating have no configured DbContext.
Do not describe a production database, gateway, or distributed deployment as implemented.

See [development](development.md) for commands and [frontend](frontend.md) for the
separate local-data UI flow.
