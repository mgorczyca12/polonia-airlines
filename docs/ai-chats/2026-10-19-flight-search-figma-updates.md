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

- None.

Previous repository session: [2026-10-17 flight schedule operating days](./2026-10-17-flight-schedule-operating-days.md).
