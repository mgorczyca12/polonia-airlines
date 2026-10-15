# polonia-airlines
A production-grade full-stack airline ecosystem for a fictional Polish flag carrier. Designed to demonstrate cloud-native system design, scalable event-driven architecture, and practical AI applications.

## Current system

The web application currently uses sample JSON data. Flight queries have API and
EF Core implementations, with SQLite supplied by integration tests rather than
normal startup. Booking and seating have domain models but no wired business APIs.

## Documentation

- [System documentation index](docs/architecture/README.md)
- [Architecture and C4 diagrams](docs/architecture/architecture.md)
- [Flight](docs/architecture/services/flight-service.md), [booking](docs/architecture/services/booking-service.md) and [seating](docs/architecture/services/seating-service.md) services
- [Frontend routes and features](docs/architecture/frontend.md)
- [Development, verification and backend code style](docs/architecture/development.md)
