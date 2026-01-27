# 054 - Transaction Location Data & Choropleth Reporting

## Goal
Enhance transaction records with location data (parsed or captured), and visualize spending patterns geographically using choropleth charts in reports.

## Motivation
- Location data enables new insights into spending habits and patterns.
- Choropleth maps provide a powerful, intuitive way to visualize geographic distribution of expenses.
- Mobile entry is an opportunity to capture precise location data at the point of transaction.

## Features


### 1. Location Data Extraction & Capture
- Parse location information from transaction descriptions using pattern matching, regular expressions, and NLP techniques.
- Maintain a list of known merchants and locations to improve parsing accuracy.
- On mobile, prompt for or auto-capture device location (with user consent) when entering new transactions.
- Allow users to manually enter or correct location data for any transaction.
- Store location as structured data (latitude/longitude, city, state, country, etc.).
- Respect privacy: allow users to opt out of location capture and control data granularity (e.g., city-level only).


### 2. Data Enrichment & Accuracy
- Use external APIs (free/open source or permissive license) to enrich or validate parsed locations (e.g., geocoding addresses or place names).
- Cross-reference parsed data with merchant/location databases for improved accuracy.
- Flag transactions with ambiguous or missing location data for user review and correction.
- Provide feedback mechanisms for users to report or fix incorrect location assignments.

### 3. Choropleth Chart Visualization
- Add choropleth map(s) to reports, showing spending by region (country, state, city, or custom area).
- Support drill-down and filtering by time, category, and other dimensions.
- Ensure maps are accessible (colorblind-friendly palettes, ARIA labels, keyboard navigation).
- Export map data and images in all supported report export formats (CSV, Excel, PDF).

### 4. Privacy & Security
- All location data collection is opt-in and clearly disclosed.
- Users can delete or anonymize location data at any time.
- Location data is never shared externally without explicit user action.

### 5. API & Data Portability
- Expose location data in API and data exports, respecting user privacy settings.
- Document location data schema in OpenAPI and export docs.

### 6. Global Location Data Setting
- Add a single application-wide settings flag (e.g., `EnableLocationData`) to control all location data features.
- If the setting is off, hide all location-related fields, inputs, and choropleth charts throughout the UI and reports.
- Ensure all location data collection, display, and export logic respects this flag.
- Setting defaults to off for new users; must be explicitly enabled.

## Out of Scope
- Real-time location tracking or background collection.
- Paid or commercial geocoding/location APIs.

## Acceptance Criteria
- Location data is parsed or captured for as many transactions as possible.
- Users can view, edit, or remove location data per transaction.
- Choropleth maps are available in reports, interactive, and accessible.
- All location features are opt-in and privacy-respecting.
- Location data is included in exports and API (if enabled by user).
- All features covered by unit/integration tests.

## Implementation Notes
- Evaluate open/free geocoding APIs (e.g., Nominatim, Photon, or similar) for enrichment.
- Use only Apache/MIT or similarly licensed libraries for map rendering and geocoding.
- Consider Blazor-compatible mapping libraries (e.g., Leaflet via JS interop, or custom SVG maps).
- Centralize location parsing and enrichment logic for maintainability.
- Add OpenAPI docs for new location fields and endpoints.

---
