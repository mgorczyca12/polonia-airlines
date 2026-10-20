# Flight search Figma updates

## Goal

Update the flight-search trip-type controls and flight-results continuation action to match the current Figma designs.

## Completed

- Replaced the flight search trip-type radios with a styled, accessible segmented control while preserving one-way/round-trip behavior and the disabled multi-city option.
- Moved “Continue to passengers” beneath the “Your journey” card in the booking sidebar, with a full-width button and centered note. Fare-selection gating remains unchanged.
- Updated the booking next-step layout for the sidebar placement.

## Files changed

- `frontend/polonia-web/src/features/flight-search/components/FlightSearch.tsx`
- `frontend/polonia-web/src/features/flight-search/styles/FlightSearch.module.scss`
- `frontend/polonia-web/src/routes/flightlist.tsx`
- `frontend/polonia-web/src/app/layout/BookingLayout.tsx`
- `frontend/polonia-web/src/app/layout/BookingLayout.module.scss`

## Verification

- `npm run build` — passed.
- Targeted Prettier check — passed.
- Targeted ESLint check — passed.
- Browser check confirmed the segmented control updates the selected trip type and the continuation button renders below the journey card.
- No matching frontend test files were found; no automated tests were run.

## Deferred

- Per-passenger fares: backend/service calls are deliberately not wired yet; the booking page only receives the chosen fare IDs (`fares=[…]`) and renders them.
- Per-passenger fares are uncommitted at the time of writing; the segmented control and continue-button work above was committed and pushed as `d7a4b5f`.

## Per-passenger fares (follow-up)

Goal: let each seated passenger hold their own fare on the selected flight, mirroring the booking-service model (one fare per passenger per flight; infants are priced at booking), with no service calls.

- Selection model is now one flight plus a fare per passenger ID (`FareSelection.fareIds`); `getSeatedPassengers` derives Adult/Child passengers from the search.
- Flight card: a checked-by-default “Use the same fare for all passengers” checkbox applies each fare to everyone. Unchecking shows a passenger selector; choosing a fare for one passenger advances to the next unassigned one, and fare cards show “Chosen by …”.
- Journey summary lists one price line per passenger, shows “Mixed fares” when they differ, and only totals once every passenger has a fare. Continue is disabled until all passengers are covered and names who is missing.
- `/booking` takes `fares` (array of fare IDs) instead of `fare`; the review page lists a fare per passenger and a total.
- Figma (`Flight results and fares`): the existing screen now shows the checked checkbox and per-passenger price lines; a duplicate screen to its right shows the unchecked state, passenger selector, “Chosen by Adult 1”, mixed fares, and updated totals. Mixed-fare totals reuse the design's tax split (£250 per adult).

Files: `search-params.ts`, `flight-search/index.ts`, `flight.helpers.ts`, `flight-results/index.ts`, `FlightResults.tsx`, `FlightResultCard.tsx`, `FareOptions.tsx` (+ scss), new `PassengerFareSelector.tsx` (+ scss), `JourneySummary.tsx`, `BookingSummary.tsx`, `routes/flightlist.tsx`, `routes/booking.tsx`.

Checks: `tsc --noEmit`, ESLint on `src/features` and `src/routes`, and `npm run build` passed. Browser run confirmed: checkbox applies one fare to all, per-passenger mixed selection totals correctly, partial selection gates Continue, and navigation to `/booking?flight=POL01&fares=[1,2,1]` renders per-passenger fares. A final element screenshot of the per-passenger card failed to capture (tool limitation), so that visual state was verified via the Figma render and DOM snapshot. No frontend unit tests exist, so none were added.

Previous repository session: [2026-10-17 flight schedule operating days](./2026-10-17-flight-schedule-operating-days.md).
