# Frontend/backend integration plan

Date: 2026-11-13

## Goal and agreed scope

Plan and implement the first real search-to-reservation web flow against the
checked-in APIs. The initial planning baseline is retained below; implementation
and final checks are recorded at the end.

- One-way first; return journeys and connections are deferred.
- Disable infant bookings for this release.
- Collect only persisted fields: passenger first name, last name and date of
  birth, plus contact name and email. Remove title, nationality, membership,
  phone, SMS and special-request inputs from the real booking flow.
- Payment is deferred until a payment service exists. Replace demo payment with
  review/submission. A completed reservation means confirmed inventory, not a
  paid booking or an issued ticket.

## Verified API inventory before implementation

| Purpose | Existing API | Status |
| --- | --- | --- |
| Search a route on a UTC calendar date | `GET /api/Flight/flights?DepartureAirport=ORD&ArrivalAirport=KRK&DepartureDate=2026-11-13&Page=1&PageSize=20` | Implemented; array response, page size 1-100, no total-count envelope |
| Reload a selected flight | `GET /api/Flight/flight/{flightId}` | Implemented; numeric service-owned ID, not flight number |
| Airport lookup | None; `AirportController` has no actions | Bundled airport labels can remain initially; empty searches must not invent schedules |
| Global active fare products | `GET /api/booking/catalog` | Implemented; no flight prices or inventory availability |
| Start guest checkout | `POST /api/booking/checkouts` | Implemented; 200 with `guestToken` |
| Create authoritative priced offer | `POST /api/booking/offers` | Implemented; 201, `X-Guest-Token`, currency, passenger count and flight/product selections |
| Reload offer | `GET /api/booking/offers/{offerId}` | Implemented; same token, expiry and ownership enforced |
| Submit reservation | `POST /api/booking/reservations` | Implemented; 201 with reservation/workflow state, same token |
| Poll/reload reservation | `GET /api/booking/reservations/{reservationId}` | Implemented; same token, real PNR, totals, fares, workflow and failure code |
| Physical seat map | `GET /api/Flight/flight/{flightId}/seat-map` | Implemented; layout only, not seat availability; not required for cabin-only booking |
| Availability, public holds, guest cancellation | No public endpoints | Not needed for initial submission; booking/seating saga owns holds, confirmation and failure compensation |
| Payment | No processing API | Deferred |

Sources: [flight controller](../../backend/flight-service/Api/Controllers/FlightController.cs),
[booking controllers](../../backend/booking-service/Api/Controllers/),
[request/response contracts](../../backend/booking-service/Application/Contracts/),
[flight contracts](../../backend/flight-service/Application/Contracts/).

## API work proposed during planning

### 1. Flight-specific fare discovery

Add a booking-owned direct query, provisionally:

`GET /api/booking/fares?FlightIds=1&FlightIds=2&Currency=USD`

- Bound and validate the flight ID list and exact uppercase currency; preserve
  opaque IDs and use deterministic ordering.
- Return explicit flight ID, product ID/code/version, cabin ID/class, brand,
  amount/currency and baggage terms for active products with actual stored prices.
- Batch the current flight-search page to avoid creating an offer for every fare
  card or issuing one price request per result.
- Empty/no-price searches succeed with an empty collection. Do not imply that
  booking's pricing database establishes flight existence or available capacity.
- Reuse catalog/price mappings, application Result DTOs, API-boundary validation
  and typed errors. Do not put prices in flight-service or add query mediators.
- Search prices are indicative. Offer creation remains authoritative and its
  returned snapshot must be reviewed before reservation submission.

### 2. Reloadable reservation summary

Extend the existing guest-scoped reservation response with contact name/email and
ordered/indexed passenger name summaries from stored reservation data. The current
response lacks these, so the confirmation UI otherwise depends on component state
after refresh. Avoid returning unnecessary passenger data. This is response/query
work, not a new persistence feature.

### 3. Adapt flight displays instead of inventing data

The current flight response has ID, number, UTC departure/arrival timestamps and
IATA airport codes. It lacks the prototype's timed segments, aircraft description,
layovers, fares and commercial perk descriptions.

- Use real IDs throughout selections; map flight-service IDs to their canonical
  invariant string representation at the booking boundary.
- Use returned timestamps and codes, with bundled airport labels where known.
- Remove unsupported connection, aircraft, lounge, meal, change/refund and mixed
  cabin claims. Do not synthesize a nonstop segment from aggregate timestamps.
- If a nonstop-only filter/label is required, define and expose an authoritative
  flight stop/segment contract first; the current response cannot establish it.
- Keep USD as an explicit first-release currency unless separately agreed.
  Children currently receive the same selected product price as adults; no
  age-based discount exists.

## Original proposed integration sequence

1. Establish coherent initial data across flight, booking prices/products and
   seating inventory using canonical flight/cabin IDs. Apply checked-in migrations
   to dedicated infrastructure. Automated flight-to-seating initial snapshot
   provisioning remains a separate task; trusted provisioning can unblock this
   integration without pretending dynamic aircraft changes are supported.
2. Implement fare discovery and the reloadable reservation summary, with focused
   API/query tests.
3. Add typed frontend API clients and same-origin routing. Existing Nginx proxies
   only to the web app and Vite has no API proxy. Proposed approach: explicitly
   route customer flight reads and booking routes through Nginx and Vite in
   production/local development respectively; do not expose flight mutation APIs
   as a side effect. Configure deployment HTTPS and forwarded-header handling.
4. Replace sample flight/fare repositories with real reads. Add loading, empty,
   pagination and explicit error states. A date-strip click can run a new search;
   omit adjacent-day price claims until those prices are actually queried.
5. Start checkout when proceeding to booking; create an offer for the selected
   flight and zero-based ordered passenger product selections. Use UUID product
   IDs instead of numeric demo fare IDs. Resolve cabin labels from actual products,
   not unverified UI string mappings.
6. Show passenger inputs and review using the offer snapshot and expiry. Handle
   expiry/unavailable prices explicitly, requiring review of any replacement offer.
7. Submit `offerId`, a stable `requestId`, contact name/email and ordered
   passengers. Preserve the same request ID and payload after ambiguous network
   failure; do not create a new booking blindly.
8. Poll secured reservation reads with bounded backoff and cancellation.
   Pending, AwaitingCapacity and ConfirmingAllocations are not confirmation.
   Compensating and RequiresIntervention need explicit recovery/support states.
   Show success only for Completed/Reserved; Cancelled is failure, not success.
   Poll timeout/network loss must retain the reservation and allow status retry,
   not imply cancellation.
9. Render the stored PNR, passenger summary, itinerary reads and authoritative
   totals. Clearly state payment is not collected and ticket issuance is absent.

Guest-token handling must be decided before implementation: prefer a server-side
web session/HttpOnly cookie if using TanStack Start as a backend-for-frontend;
otherwise assess tab-scoped token storage for a narrowly routed browser client.
Both must support refresh/retry safely. Never put guest tokens or passenger data
in URLs, logs, analytics or cached responses. A cookie-based approach also needs
CSRF protection; do not add a backend-for-frontend without choosing its session
storage and deployment model.

Keep the existing frontend maximum of eight seated travellers initially (the
backend accepts nine); do not expand that limit as incidental integration work.
Reject/deactivate unsupported infant/return options, including stale URL state,
instead of silently booking a different journey.

## Acceptance checks proposed during planning

- Real flight search, fare discovery, per-passenger products and persisted PNR
  confirmation with durable seating allocations.
- Empty route/prices, invalid input, expired offer, price change, insufficient
  capacity and compensation states.
- Duplicate submission and ambiguous-response retry produce one reservation.
- Refresh resumes offer/reservation reads; wrong guest cannot access either.
- Backend unavailable does not fall back to sample data or fake confirmation.
- Frontend build/type checks and focused component/API tests; a dedicated
  infrastructure browser end-to-end case for success and inventory rejection.

## Initial planning changes and checks

- Created this plan/session record only; no application code, contracts, schema,
  infrastructure or GitHub resources were changed.
- Reviewed the concrete controllers, DTOs, validators, offer handler, reservation
  query, flight query/domain, web routes/models, and Nginx/Vite/Compose config.
- Initial `git status --short`: clean; final status showed only this new file.
- `git diff --check`: passed for tracked changes (the plan is untracked).
  All relative documentation link targets were checked and exist.
- No builds, tests, migrations, service startup or live API calls were performed.
- Open GitHub work could not be checked earlier in this session because `gh` is
  unauthenticated; the API inventory is based on local checked-in code.

## Implementation decisions and completed changes

- The user selected a TanStack Start server-side API layer with an encrypted
  HttpOnly guest cookie. This supersedes the proposed direct Nginx/Vite backend
  routing; existing Nginx web proxying is sufficient.
- Added booking-owned `GET /api/booking/fares` with validated batches of 1-100
  exact flight references, actual active product prices, and deterministic
  ordering. Empty price results remain successful empty collections.
- Added stored contact/passenger names to guest-scoped reservation reads and
  `GET /api/booking/reservations/by-request/{requestId}` for lost-response recovery.
  No schema changes or new migrations were needed.
- Added typed frontend API clients, runtime response guards, and the allowlisted
  server layer using `iron-session`. It enforces exact-Origin mutation checks,
  encoded-byte body limits, cancellation/timeouts, safe errors and no-store
  responses. Guest tokens remain server-side and are stripped from public DTOs.
- Connected search, batched fares, per-passenger selections, offer review,
  passenger/contact collection, idempotent submission and bounded polling.
  Submission identities use the offer GUID's canonical casing, so differently
  cased offer URLs recover the same booking.
- Replaced payment with review and unpaid reservation submission. Confirmation
  requires both Reserved and Completed. Cancellation, pending inventory,
  intervention, expiry and reload errors are explicit.
- Removed sample flight/pricing fallback and unsupported commercial claims from
  integrated screens. Adjacent-day prices and nonstop metadata are omitted.
  One-way, USD, eight seated travellers and no infants remain the agreed scope.
- Updated server environment examples, Compose wiring, root/service READMEs,
  frontend/booking architecture, diagrams and development setup.

Key files:
[web API/session layer](../../frontend/polonia-web/src/shared/api/web-api.server.ts),
[API contracts/client](../../frontend/polonia-web/src/shared/api/),
[flight search route](../../frontend/polonia-web/src/routes/flightlist.tsx),
[booking UI](../../frontend/polonia-web/src/features/booking/components/LiveBooking.tsx),
[fare controller](../../backend/booking-service/Api/Controllers/FareController.cs),
[reservation queries](../../backend/booking-service/Infrastructure/Persistence/Queries/ReservationQueries.cs),
[booking API tests](../../backend/tests/integration/booking-service.integration.test/BookingApiTests.cs).

## Implementation checks and actual results

- Focused booking API/checkout Release tests: **13 passed**. Covered fare filtering,
  validation and the exact 100/101 batch boundary, stored summaries, guest
  isolation, and request-ID recovery.
- Full solution Release build: **passed**, zero errors; one pre-existing CS9113
  warning about the aircraft controller's unread mediator parameter.
- Backend whitespace and IDE0011/IDE0021/IDE0022 style verification: **passed**.
- Frontend tests: **18 passed**; TypeScript and changed-file ESLint: **passed**.
  Production client/server build: **passed**. New component tests initially
  needed jsdom `matchMedia` setup and a type-import lint fix; both are resolved.
- Client build scan found no server session-password, upstream API URL,
  guest-cookie or guest-header implementation strings.
- Frozen dependency installation: **passed**. The lockfile adds only
  `iron-session` and its three transitive dependencies; unrelated resolutions
  were preserved.
- Compose configuration validation with process-only dummy values: **passed**;
  it did not start or modify infrastructure.
- Browser checks against an isolated synthetic API fixture: success and real
  router rendering, cancellation without false confirmation, stored summary
  after refresh, and recovery after deliberately losing a committed submission
  response all **passed**. At a 390-pixel viewport there was no horizontal
  overflow; JavaScript could not read the guest cookie.
- Related documentation link targets and `git diff --check`: **passed**.
  Temporary browser web/API processes were stopped and helper scripts removed.

## Deferred work and limitations

- Docker was initially unavailable; the backend Docker tests were subsequently
  run successfully as recorded below. Full browser-to-PostgreSQL/Redis/RabbitMQ
  verification was **not performed**; the real backend tests do not exercise the
  web UI or flight-service.
- Dedicated infrastructure still needs checked-in schemas and coherent actual
  flight, price/product and seating data. Automatic flight-to-seating provisioning
  remains separate work.
- Production authentication/authorization for existing administration APIs
  remains pending; these endpoints are not exposed by the new web API layer.
- Payment service/integration, tickets/email, returns/connections and infants
  remain deferred.
- No migrations or business data changes were applied outside isolated test
  fixtures. No commits or pushes were made.

## Docker-backed verification follow-up

After the user started Docker, ran the existing external test categories against
three disposable PostgreSQL 18, Redis 8 and RabbitMQ 4 management containers.
Ports were dynamically allocated and bound to localhost; passwords were generated
in process memory. No existing containers or shared infrastructure were used.

```powershell
dotnet test polonia-airlines.slnx --configuration Release --no-build --no-restore --filter 'Category=ExternalInventory|Category=ExternalWorkflow'
```

- **10 passed, 0 failed, 0 skipped**: five seating inventory cases and five
  booking broker-workflow/crash-recovery cases.
- Verified live allocation/cleanup, successful reservation completion, capacity
  rejection/compensation, idempotent replay and the existing process-recovery
  scenarios through production backend hosts.
- Fixtures applied existing migrations and provisioned synthetic data only in
  their dedicated test databases, then cleaned their resources. All three owned
  containers were removed after the run.
- This did not run the web browser against actual flight-service and booking
  hosts. That separate frontend end-to-end check remains pending.

## System-test organization discussion

- Agreed to call full end-to-end tests **system tests** and place them in a new
  root `tests/system/` directory. Backend unit/integration tests remain under
  `backend/tests/`; frontend component/client tests remain near their code.
- Proposed Playwright plus an isolated Docker Compose stack, real service
  migrations and coordinated synthetic flight, fare and inventory fixtures.

## System-test setup

- At the user's request, added [tests/system](../../tests/system/README.md):
  Playwright scenarios for a confirmed booking and an insufficient-capacity
  cancellation, plus checks of durable seating allocations.
- Added a standalone Compose stack with a unique per-run project, ephemeral test
  credentials and storage, loopback-only web port, production service images,
  a fixture that applies existing migrations and seeds coherent synthetic data,
  business-read readiness checks, failure logs and scoped cleanup.
- Pinned Playwright with its own lockfile. Frozen installation, fixture Release
  build, JavaScript syntax checks, Playwright module load and Compose config
  validation passed. No test stack was started during this setup.
- **The new system tests were not run**, as requested. Browser behavior, fixture
  execution and cleanup remain unverified in a live run. Automatic CI and any
  system-test-specific dependency automation are deferred until after validation.
