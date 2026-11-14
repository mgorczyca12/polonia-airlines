---
description: "Use when creating, editing, or reviewing backend services, shared backend code, or backend tests. Covers layering, CQRS, query results and API errors, domain encapsulation, cross-service identifiers, service structure, and backend patterns."
applyTo: "backend/**"
---

# Backend architecture

Formatting rules are in [backend-style.instructions.md](backend-style.instructions.md).

## Architecture preferences
- Use DDD (Domain-Driven Design) / Clean Architecture / Hexagonal Architecture principles for structuring the application.
  - 'Api' for the API layer, handling HTTP requests and responses.
  - 'Domain' for the core business logic and domain entities.
  - 'Application' for application services and use cases.
  - 'Infrastructure' for external systems, databases, and other technical concerns.
- Keep reads and writes separated using CQRS (Command Query Responsibility Segregation)
  - Commands go through mediator handlers
  - Queries stay in query services/read models and should not be mixed into command handlers
- Keep controllers thin. Delegate commands via mediator and queries to application query interfaces; avoid business logic.

## Query results and API errors
- Commands use DispatchR handlers and validation pipelines; queries call application query interfaces/read models directly, without introducing mediator handlers solely for validation or error handling.
- Query interfaces return `FluentResults.Result<T>` over application response DTOs, not domain entities, EF entities, or HTTP types. Application must not depend on Infrastructure.
- Explicitly validate direct query inputs with FluentValidation at the API boundary before execution; command pipelines do not run for direct queries. Reuse application-level validation when adding non-HTTP callers.
- Return typed application errors with stable codes for expected outcomes such as a missing flight. Discover not-found during the actual read, not through a separate existence check in an input validator.
- Controllers translate successful result values to response bodies and expected errors to appropriate HTTP statuses with ProblemDetails/ValidationProblemDetails. Never serialize FluentResults itself or match errors by message text.
- Empty searches succeed with an empty collection. Invalid input returns 400; missing resources return 404. Unrecognized result failures must surface explicitly, not become successful responses.
- Let unexpected infrastructure/programming exceptions propagate to centralized API exception handling. Log full exceptions internally; return safe 500 ProblemDetails with a trace identifier, never raw exception messages or stack traces. Do not broadly map programming exceptions to client errors.
- Pass cancellation tokens through all async boundaries; request cancellation is not an application failure.
- Store service timestamps in UTC and expose UTC timestamps with explicit offsets. Frontends/downstream applications own local-time conversion. Calendar-date search filters use UTC day boundaries.
- Validate pagination bounds, avoid offset overflow, and use deterministic ordering before pagination. Airport search inputs use exact uppercase IATA codes, without silent normalization.

## Domain encapsulation
- Protect aggregate invariants at aggregate boundaries. Meaningful state changes should be performed through domain methods rather than public setters.
- Use private setters for entity state that must be controlled, while allowing simple descriptive/configuration data to remain straightforward for EF Core persistence.
- Aggregate roots own and mutate child collections. Expose collections as read-only when practical, and initialize EF Core navigation collections.
- Do not force every property change through a method. Add domain methods where a change has business meaning, validates a transition, or raises a domain event.
- Keep identity/equality separate from event capture. Entities may use different identifier types, and only aggregate roots need domain-event support.

## Cross-service identifiers
- Treat IDs owned by another service as opaque strings in domain references and application contracts. Do not assume or parse the owning service's internal ID type.
- Keep service-owned identities and relationships within the same service in their native types.
- Application/integration boundaries map contracts to domain references using the owning service's canonical representation.
- Validate external references as non-null and non-whitespace, but preserve their supplied representation. Do not silently trim, change case, or otherwise normalize them; compare using ordinal, case-sensitive semantics unless the owning contract explicitly specifies otherwise.
- External references do not introduce cross-service entity navigation or database foreign keys.

## Service project structure
Organize each service using the following structure:

```text
<service-name>/
  Api/
    Controllers/
    Extensions/
  Application/
    Abstractions/
    Contracts/
      Requests/
      Responses/
    Features/
      <FeatureName>/
        Commands/
        Queries/
  Domain/
    Entities/
    ValueObjects/
    Events/
  Infrastructure/
    Persistence/
    Repositories/
    Integrations/
```

- `Api` contains HTTP-specific concerns only. Place controllers in `Controllers` and service-registration, middleware, and endpoint configuration in `Extensions`.
- `Application` contains use cases and application contracts. Each feature owns its commands and queries; commands are handled through the mediator, while queries use query services or read models.
- `Domain` contains framework-independent (to the best extent possible) business rules, entities, value objects, and domain events.
- `Infrastructure` implements application abstractions and contains database access, repository implementations, and external-system integrations.

## Backend patterns and tools in use
- Mediator pattern via 'DispatchR'.
- CQRS with separate command handlers and query services
- Result pattern via 'FluentResults'
- Command Validation via 'FluentValidation'
- Event processing, Inbox, and Outbox via MassTransit
- Repository pattern for data access and persistence
- Dependency Injection through per-layer 'DependencyInjection' static extension classes that extend WebApplicationBuilder, ensuring each layer registers its own dependencies independently.
- OpenTelemetry for distributed tracing and observability
- Middleware for handling cross-cutting concerns such as logging, authentication, and error handling, and other global application concerns
- Mediator (DispatchR) pipelines for handling input validation, transaction management, and other request-level concerns
