# Copilot Instructions

## Project Guidlines

## Architecture Preferences
- Use DDD (Domain-Driven Design) / Clean Architecture / Hexagonal Architecture principles for structuring the application.
  - 'Api' for the API layer, handling HTTP requests and responses.
  - 'Domain' for the core business logic and domain entities.
  - 'Application' for application services and use cases.
  - 'Infrastructure' for external systems, databases, and other technical concerns.
- Keep reads and writes separated using CQRS (Command Query Responsibility Segregation)
  - Commands go through mediator handlers
  - Queries stay in query services/read models and should not be mixed into command handlers
- Keep controllers thin. They should delegate to application service via mediator and avoid business logic

## Service Project Structure
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

## Frontend Platform Strategy
- Treat the current customer-facing web application as the primary frontend. It must be responsive across desktop and mobile browsers.
- Use implementation-neutral platform names for new applications: `polonia-web`, `polonia-desktop`, and `polonia-mobile`.
- Create `polonia-desktop` only when native desktop capabilities or an operations-focused workflow require them, such as offline support, kiosk mode, native printing, or hardware integration.
- Create `polonia-mobile` only when native mobile capabilities require App Store or Play Store distribution, push notifications, wallet boarding passes, offline itineraries, or deep device integrations.
- When multiple frontend applications exist, share non-visual code through frontend packages: `api-client` for API contracts and clients, `domain` for frontend domain types and rules, and `ui` for design tokens and reusable UI primitives.
- Share API clients, domain types, and design tokens across web and native applications, but use UI components appropriate to each platform.

## Backend Patterns and Tools In Use
- Mediator pattern via 'DispatchR'.
- CQRS with seperate command handlers and query sevices
- Result pattern via 'FluentResults'
- Command Validation via 'FluentValidation'
- Event processing, Inbox, and Outbox via MassTransit
- Repository pattern for data access and persistence
- Dependency Injection through per-layer 'DependencyInjection' static extension classes that extend WebApplicationBuilder, ensuring each layer registers its own dependencies independently.
- OpenTelemetry for distributed tracing and observability
- Middleware for handling cross-cutting concerns such as logging, authentication, and error handling, and other global application concerns
- Mediator (DispatchR) pipelines for handling input validation, transaction management, and other request-level concerns
