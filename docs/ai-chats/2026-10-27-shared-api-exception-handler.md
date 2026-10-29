# Shared API exception handler

Previous session context: [2026-10-23 Dependabot automation diagnosis](./2026-10-23-dependabot-automation-diagnosis.md).

## Goal and decision

Move the flight API exception handler into the shared project after confirming it contains no flight-service-specific behavior. It handles generic FluentValidation exceptions and ASP.NET Core problem details; its previous flight-service namespace was the only service-specific reference.

## Completed

- Moved `ApiExceptionHandler` to `backend/shared/Api/Middleware` under `Shared.Api.Middleware`.
- Added the ASP.NET Core framework reference to the shared project and updated the flight API registration and integration-test import.
- Preserved the existing handler behavior and flight API problem-details configuration.

## Checks

- Flight-service integration tests: passed (43/43).
- Full solution build: passed with 0 warnings and 0 errors.
- `dotnet format whitespace ... --verify-no-changes`: passed.
- Required `dotnet format style` diagnostics: passed.
- `git diff --check`: passed.

## Deferred

- Other services can register the shared handler when they adopt the same API exception handling behavior.

## Guest reservation contact model

Decision: reservations may be created without a registered account. `UserId` is optional, while contact name and email are required and stored on the reservation so the booking retains the responsible customer's details.

Implemented `Reservation.Create` changes and added unit coverage for guest reservations, normalized contact values, and invalid contact data. The focused booking unit-test command is currently blocked by pre-existing in-progress booking API edits: `ReservationController` references missing request/response types and `Range` is unresolved. No changes were made to those API edits.

Changed files: `backend/booking-service/Domain/Entities/Reservation.cs` and `backend/tests/unit/booking-service.unit.test/ReservationTests.cs`.

Checks for this change: required style diagnostics passed and `git diff --check` passed. The whitespace formatter reports a missing final newline in the already-modified `ReservationController.cs`; that file was left untouched. Booking unit tests could not run because the existing API compilation errors prevent the project from building.
