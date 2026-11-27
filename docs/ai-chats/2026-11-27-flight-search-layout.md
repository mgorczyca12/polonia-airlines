# Flight search layout

- Goal: keep departure and travellers below the airport fields on wide screens.
- Changed the flight search grid to use the existing two-row desktop layout at all desktop widths; the mobile stacked layout remains unchanged.
- Aligned the departure date and travellers fields with the airport fields by leaving the swap-button column empty on the second row.
- Changed: `frontend/polonia-web/src/features/flight-search/ui/FlightSearch.module.scss`.
- Validation: the changed stylesheet passed Prettier after formatting. Other frontend checks and browser verification were not run.
- Pre-existing change: `frontend/polonia-web/src/routeTree.gen.ts` was already modified and was not intentionally changed.
