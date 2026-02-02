# 054 - Transaction Location Data & Choropleth Reporting
> **Status:** 🗒️ Planning

## Overview

This feature adds geographic location data to transactions and provides choropleth map visualizations to help users understand where their money is being spent. Location data can be captured automatically on mobile devices, parsed from transaction descriptions, or manually entered. A dedicated reports page will display spending patterns by geographic region using interactive maps.

## Problem Statement

### Current State

- Transactions have no location data beyond what might be embedded in the description text (e.g., "AMAZON.COM SEATTLE WA").
- Users cannot visualize spending patterns geographically.
- No way to answer questions like "How much do I spend when traveling?" or "Which regions have my highest expenses?"
- Mobile transaction entry doesn't leverage device GPS capabilities.

### Target State

- All transactions can have optional structured location data (coordinates, city, state/region, country).
- A new "Spending by Location" report shows choropleth maps with drill-down capability.
- Location parsing extracts geographic data from imported transaction descriptions.
- Mobile users can capture GPS coordinates when entering transactions.
- Location features are opt-in and privacy-respecting with a global toggle in settings.

---

## User Stories

### Location Data Capture

#### US-054-001: Manual Location Entry
**As a** user  
**I want to** manually add or edit location data for a transaction  
**So that** I can accurately track where my spending occurs

**Acceptance Criteria:**
- [ ] Transaction edit form includes optional location fields (city, state/region, country)
- [ ] Location fields only visible when `EnableLocationData` setting is enabled
- [ ] Changes persist and appear in transaction details
- [ ] Location can be cleared/removed

#### US-054-002: GPS Capture on Mobile
**As a** mobile user  
**I want to** capture my current GPS location when entering a transaction  
**So that** I don't have to manually type location details

**Acceptance Criteria:**
- [ ] "Use Current Location" button appears on transaction create form (mobile only)
- [ ] Browser prompts for geolocation permission
- [ ] Coordinates are reverse-geocoded to city/state/country
- [ ] User can override auto-captured location
- [ ] Graceful handling when GPS unavailable or denied

#### US-054-003: Parse Location from Description
**As a** user importing transactions  
**I want** the system to automatically extract location data from transaction descriptions  
**So that** imported transactions have location data without manual entry

**Acceptance Criteria:**
- [ ] Location parser identifies city/state patterns (e.g., "SEATTLE WA", "NEW YORK, NY")
- [ ] Common abbreviations and formats are recognized
- [ ] Parsed locations are flagged as "auto-detected" vs "confirmed"
- [ ] Users can review and correct auto-detected locations

### Location Settings

#### US-054-004: Global Location Toggle
**As a** privacy-conscious user  
**I want to** enable or disable all location features with a single setting  
**So that** I have full control over whether location data is collected or displayed

**Acceptance Criteria:**
- [ ] `EnableLocationData` setting in AppSettings (defaults to `false`)
- [ ] When disabled: hide all location fields, parsing, and map reports
- [ ] When enabled: show location features throughout the app
- [ ] Setting change takes effect immediately without refresh
- [ ] Clear messaging about what the setting controls

#### US-054-005: Delete All Location Data
**As a** user  
**I want to** bulk-delete all location data from my transactions  
**So that** I can remove historical location information for privacy

**Acceptance Criteria:**
- [ ] "Delete All Location Data" button in settings (when feature enabled)
- [ ] Confirmation dialog explaining the action is irreversible
- [ ] All transaction location fields set to null
- [ ] Success notification shown

### Choropleth Visualization

#### US-054-006: Spending by State/Region Map
**As a** user  
**I want to** see a map showing my spending by state or region  
**So that** I can visualize geographic spending patterns

**Acceptance Criteria:**
- [ ] New "Spending by Location" report page
- [ ] Interactive US state map (MVP) with color intensity based on spending
- [ ] Hover shows state name and total spending
- [ ] Click drills down to city-level data for that state
- [ ] Date range filter matches other reports

#### US-054-007: Accessible Map Visualization
**As a** user with visual impairments  
**I want** the choropleth map to be accessible  
**So that** I can understand spending patterns with assistive technology

**Acceptance Criteria:**
- [ ] Colorblind-friendly palette with pattern fills as alternative
- [ ] Screen reader announces region names and values
- [ ] Keyboard navigation between regions
- [ ] Data table alternative view available

#### US-054-008: Export Location Report Data
**As a** user  
**I want to** export the location spending data  
**So that** I can use it in other tools or keep records

**Acceptance Criteria:**
- [ ] CSV export includes region, spending, and transaction count
- [ ] Export respects current filters (date range, category)
- [ ] PDF export includes map image and data table

---

## Technical Design

### Architecture Changes

```
┌─────────────────────────────────────────────────────────────────────┐
│                          Client (Blazor WASM)                       │
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐  │
│  │ LocationFields   │  │ ChoroplethMap    │  │ GeolocationService│  │
│  │ Component        │  │ Component        │  │ (JS Interop)     │  │
│  └──────────────────┘  └──────────────────┘  └──────────────────┘  │
└─────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────┐
│                              API                                    │
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐  │
│  │ TransactionsCtrl │  │ ReportsController│  │ GeocodingCtrl    │  │
│  │ + Location DTOs  │  │ + LocationReport │  │ (reverse lookup) │  │
│  └──────────────────┘  └──────────────────┘  └──────────────────┘  │
└─────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────┐
│                          Application                                │
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐  │
│  │ LocationParser   │  │ GeocodingService │  │ LocationReport   │  │
│  │ Service          │  │                  │  │ Service          │  │
│  └──────────────────┘  └──────────────────┘  └──────────────────┘  │
└─────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────┐
│                            Domain                                   │
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐  │
│  │ TransactionLoca- │  │ GeoCoordinate    │  │ AppSettings      │  │
│  │ tion (ValueObj)  │  │ (ValueObject)    │  │ +EnableLocation  │  │
│  └──────────────────┘  └──────────────────┘  └──────────────────┘  │
└─────────────────────────────────────────────────────────────────────┘
```

### Domain Model

#### New Value Objects

```csharp
// GeoCoordinate.cs - Immutable latitude/longitude pair
public sealed class GeoCoordinate : IEquatable<GeoCoordinate>
{
    public decimal Latitude { get; }   // -90 to 90
    public decimal Longitude { get; }  // -180 to 180
    
    private GeoCoordinate(decimal latitude, decimal longitude) { ... }
    
    public static GeoCoordinate Create(decimal latitude, decimal longitude)
    {
        // Validate ranges, throw DomainException if invalid
    }
}

// TransactionLocation.cs - Full location data
public sealed class TransactionLocation : IEquatable<TransactionLocation>
{
    public GeoCoordinate? Coordinates { get; }
    public string? City { get; }
    public string? StateOrRegion { get; }
    public string? Country { get; }          // ISO 3166-1 alpha-2 (e.g., "US")
    public string? PostalCode { get; }
    public LocationSource Source { get; }   // Manual, GPS, Parsed, Geocoded
    
    public static TransactionLocation Create(...) { ... }
    public static TransactionLocation CreateFromParsed(string city, string state) { ... }
    public static TransactionLocation CreateFromGps(GeoCoordinate coords) { ... }
}

// LocationSource.cs - Enum
public enum LocationSource
{
    Manual = 0,     // User typed it
    Gps = 1,        // Device GPS capture
    Parsed = 2,     // Extracted from description
    Geocoded = 3    // Enriched via geocoding API
}
```

#### Transaction Entity Changes

```csharp
// Add to Transaction.cs
public TransactionLocation? Location { get; private set; }

public void SetLocation(TransactionLocation? location)
{
    Location = location;
    UpdatedAt = DateTime.UtcNow;
}

public void ClearLocation()
{
    Location = null;
    UpdatedAt = DateTime.UtcNow;
}
```

#### AppSettings Changes

```csharp
// Add to AppSettings.cs
public bool EnableLocationData { get; private set; } = false;

public void SetEnableLocationData(bool enabled)
{
    EnableLocationData = enabled;
    UpdatedAtUtc = DateTime.UtcNow;
}
```

### API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| PATCH | `/api/v1/transactions/{id}/location` | Update transaction location |
| DELETE | `/api/v1/transactions/{id}/location` | Remove transaction location |
| POST | `/api/v1/geocoding/reverse` | Reverse geocode coordinates to address |
| GET | `/api/v1/reports/spending-by-location` | Get location spending report data |
| DELETE | `/api/v1/settings/location-data` | Bulk delete all location data |

#### New DTOs

```csharp
// TransactionLocationDto.cs
public sealed class TransactionLocationDto
{
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? City { get; set; }
    public string? StateOrRegion { get; set; }
    public string? Country { get; set; }
    public string? PostalCode { get; set; }
    public string? Source { get; set; }  // "Manual", "GPS", "Parsed", "Geocoded"
}

// TransactionLocationUpdateDto.cs
public sealed class TransactionLocationUpdateDto
{
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? City { get; set; }
    public string? StateOrRegion { get; set; }
    public string? Country { get; set; }
    public string? PostalCode { get; set; }
}

// ReverseGeocodeRequestDto.cs
public sealed class ReverseGeocodeRequestDto
{
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
}

// ReverseGeocodeResponseDto.cs
public sealed class ReverseGeocodeResponseDto
{
    public string? City { get; set; }
    public string? StateOrRegion { get; set; }
    public string? Country { get; set; }
    public string? PostalCode { get; set; }
    public string? FormattedAddress { get; set; }
}

// LocationSpendingReportDto.cs
public sealed class LocationSpendingReportDto
{
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal TotalSpending { get; set; }
    public int TotalTransactions { get; set; }
    public int TransactionsWithLocation { get; set; }
    public List<RegionSpendingDto> Regions { get; set; } = new();
}

// RegionSpendingDto.cs
public sealed class RegionSpendingDto
{
    public string RegionCode { get; set; } = string.Empty;  // e.g., "US-WA"
    public string RegionName { get; set; } = string.Empty;  // e.g., "Washington"
    public string Country { get; set; } = string.Empty;
    public decimal TotalSpending { get; set; }
    public int TransactionCount { get; set; }
    public decimal Percentage { get; set; }
    public List<CitySpendingDto>? Cities { get; set; }  // For drill-down
}

// CitySpendingDto.cs
public sealed class CitySpendingDto
{
    public string City { get; set; } = string.Empty;
    public decimal TotalSpending { get; set; }
    public int TransactionCount { get; set; }
    public decimal Percentage { get; set; }
}
```

### Database Changes

#### Migration: Add Location Columns to Transactions

```sql
ALTER TABLE "Transactions" ADD COLUMN "Location_Latitude" DECIMAL(9,6) NULL;
ALTER TABLE "Transactions" ADD COLUMN "Location_Longitude" DECIMAL(9,6) NULL;
ALTER TABLE "Transactions" ADD COLUMN "Location_City" VARCHAR(100) NULL;
ALTER TABLE "Transactions" ADD COLUMN "Location_StateOrRegion" VARCHAR(100) NULL;
ALTER TABLE "Transactions" ADD COLUMN "Location_Country" CHAR(2) NULL;
ALTER TABLE "Transactions" ADD COLUMN "Location_PostalCode" VARCHAR(20) NULL;
ALTER TABLE "Transactions" ADD COLUMN "Location_Source" INTEGER NULL;

-- Index for location-based queries
CREATE INDEX "IX_Transactions_Location_Country_State" 
ON "Transactions" ("Location_Country", "Location_StateOrRegion")
WHERE "Location_Country" IS NOT NULL;
```

#### Migration: Add EnableLocationData to AppSettings

```sql
ALTER TABLE "AppSettings" ADD COLUMN "EnableLocationData" BOOLEAN NOT NULL DEFAULT FALSE;
```

### UI Components

#### New Components

| Component | Location | Purpose |
|-----------|----------|---------|
| `LocationFields.razor` | Client/Components/Transactions | Reusable location input fields |
| `LocationDisplay.razor` | Client/Components/Transactions | Read-only location display |
| `ChoroplethMap.razor` | Client/Components/Reports | Interactive SVG/Canvas map |
| `LocationReportPage.razor` | Client/Pages/Reports | Full report page with map + filters |
| `GeolocationService.cs` | Client/Services | JS interop for GPS access |
| `leaflet-interop.js` | Client/wwwroot/js | Leaflet.js wrapper (optional) |

#### Map Library Decision

**Recommended: Custom SVG Maps (MIT Licensed)**

For MVP, use pre-built SVG maps of US states rather than a full mapping library:
- Simpler integration (no JS interop complexity)
- Smaller bundle size
- Full control over styling and accessibility
- Easy to make responsive

SVG assets available from [simplemaps.com](https://simplemaps.com/resources/svg-us) (MIT License) or similar sources.

**Future Enhancement:** Integrate Leaflet.js via JS interop for world maps and drill-down to city level with actual geographic positioning.

### Location Parser Design

```csharp
public interface ILocationParserService
{
    TransactionLocation? ParseFromDescription(string description);
    IReadOnlyList<LocationParseResult> ParseBatch(IEnumerable<string> descriptions);
}

public sealed class LocationParseResult
{
    public string OriginalText { get; init; }
    public TransactionLocation? Location { get; init; }
    public decimal Confidence { get; init; }  // 0.0 to 1.0
    public string? MatchedPattern { get; init; }
}
```

**Common Patterns to Match:**
- `CITY STATE` (e.g., "SEATTLE WA")
- `CITY, STATE` (e.g., "Seattle, WA")  
- `CITY STATE ZIP` (e.g., "SEATTLE WA 98101")
- US state abbreviations (all 50 + DC, territories)
- Canadian province abbreviations
- Common city name normalization

### Geocoding Service Design

```csharp
public interface IGeocodingService
{
    Task<ReverseGeocodeResult?> ReverseGeocodeAsync(
        decimal latitude, 
        decimal longitude, 
        CancellationToken cancellationToken = default);
}

// Implementation uses Nominatim (OpenStreetMap) - free, no API key required
// Rate limit: 1 request/second, respect usage policy
public sealed class NominatimGeocodingService : IGeocodingService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NominatimGeocodingService> _logger;
    
    // Implement with rate limiting and caching
}
```

---

## Implementation Plan

### Phase 1: Domain Model & Value Objects

**Objective:** Create domain value objects for location data with full validation.

**Tasks:**
- [ ] Create `GeoCoordinate` value object with lat/lng validation
- [ ] Create `LocationSource` enum
- [ ] Create `TransactionLocation` value object with factory methods
- [ ] Add `Location` property to `Transaction` entity
- [ ] Add `EnableLocationData` to `AppSettings`
- [ ] Write unit tests for all value object validation rules
- [ ] Write unit tests for `Transaction.SetLocation()` and `ClearLocation()`

**Commit:**
```bash
git commit -m "feat(domain): add transaction location value objects

- Add GeoCoordinate value object with coordinate validation
- Add TransactionLocation value object with source tracking
- Add LocationSource enum (Manual, GPS, Parsed, Geocoded)
- Extend Transaction entity with Location property
- Add EnableLocationData setting to AppSettings

Refs: #054"
```

---

### Phase 2: Infrastructure & Database Migration

**Objective:** Persist location data with EF Core configuration.

**Tasks:**
- [ ] Create EF Core migration for location columns on Transactions
- [ ] Create migration for AppSettings.EnableLocationData
- [ ] Configure owned entity mapping for `TransactionLocation`
- [ ] Add index for location-based queries
- [ ] Write integration tests for location persistence
- [ ] Test migration up/down scripts

**Commit:**
```bash
git commit -m "feat(infrastructure): add location data persistence

- Add migration for Transaction location columns
- Add migration for AppSettings.EnableLocationData
- Configure TransactionLocation as owned entity
- Add composite index on Country/StateOrRegion

Refs: #054"
```

---

### Phase 3: Location DTOs & API Endpoints

**Objective:** Expose location data via API with proper DTOs.

**Tasks:**
- [ ] Create location-related DTOs in Contracts
- [ ] Add `Location` property to `TransactionDto`
- [ ] Add `LocationUpdateDto` for PATCH endpoint
- [ ] Create PATCH `/transactions/{id}/location` endpoint
- [ ] Create DELETE `/transactions/{id}/location` endpoint
- [ ] Update OpenAPI documentation
- [ ] Write API integration tests

**Commit:**
```bash
git commit -m "feat(api): add transaction location endpoints

- Add TransactionLocationDto and update DTOs
- Add PATCH endpoint to update transaction location
- Add DELETE endpoint to clear transaction location
- Update OpenAPI docs with location schemas

Refs: #054"
```

---

### Phase 4: Location Parser Service

**Objective:** Parse location from transaction descriptions.

**Tasks:**
- [ ] Create `ILocationParserService` interface
- [ ] Implement regex-based location parsing for US/CA formats
- [ ] Build state/province abbreviation lookup dictionary
- [ ] Handle common city name variations
- [ ] Return confidence scores for matches
- [ ] Write extensive unit tests with real-world examples
- [ ] Integrate parser into import pipeline (optional call)

**Commit:**
```bash
git commit -m "feat(application): add transaction location parser

- Add ILocationParserService interface
- Implement regex patterns for US/CA location formats
- Include state/province abbreviation dictionaries
- Return confidence scores for parsed locations
- Add to import pipeline as optional enrichment

Refs: #054"
```

---

### Phase 5: Geocoding Service Integration

**Objective:** Reverse geocode GPS coordinates to addresses.

**Tasks:**
- [ ] Create `IGeocodingService` interface
- [ ] Implement Nominatim (OpenStreetMap) adapter
- [ ] Add rate limiting (1 req/sec) and caching
- [ ] Create POST `/geocoding/reverse` API endpoint
- [ ] Handle API errors gracefully
- [ ] Write integration tests with mocked HTTP responses

**Commit:**
```bash
git commit -m "feat(infrastructure): add Nominatim geocoding service

- Add IGeocodingService interface
- Implement Nominatim reverse geocoding adapter
- Add rate limiting and response caching
- Add POST /geocoding/reverse endpoint
- Respect OpenStreetMap usage policy

Refs: #054"
```

---

### Phase 6: Client Location Components

**Objective:** Build UI components for viewing and editing location data.

**Tasks:**
- [ ] Create `LocationFields.razor` component (city/state/country inputs)
- [ ] Create `LocationDisplay.razor` component (read-only view)
- [ ] Add `GeolocationService` for GPS capture via JS interop
- [ ] Integrate location fields into transaction edit form
- [ ] Conditionally show location UI based on `EnableLocationData` setting
- [ ] Write component unit tests with bUnit

**Commit:**
```bash
git commit -m "feat(client): add transaction location UI components

- Add LocationFields.razor for editing location data
- Add LocationDisplay.razor for read-only display
- Add GeolocationService for GPS capture via JS interop
- Integrate into transaction edit form
- Respect EnableLocationData setting visibility

Refs: #054"
```

---

### Phase 7: Location Report Service

**Objective:** Aggregate spending data by geographic region.

**Tasks:**
- [ ] Create `ILocationReportService` interface
- [ ] Implement spending aggregation by state/region
- [ ] Support date range filtering
- [ ] Calculate percentages and totals
- [ ] Create GET `/reports/spending-by-location` endpoint
- [ ] Write unit tests for aggregation logic
- [ ] Write API integration tests

**Commit:**
```bash
git commit -m "feat(application): add location spending report service

- Add ILocationReportService interface
- Implement spending aggregation by region
- Support date range and category filters
- Add GET /reports/spending-by-location endpoint
- Return RegionSpendingDto with drill-down data

Refs: #054"
```

---

### Phase 8: Choropleth Map Component

**Objective:** Build interactive SVG map visualization.

**Tasks:**
- [ ] Add US states SVG map asset (MIT licensed)
- [ ] Create `ChoroplethMap.razor` component
- [ ] Implement color scale based on spending values
- [ ] Add hover tooltips with region details
- [ ] Add click handler for drill-down
- [ ] Implement keyboard navigation between regions
- [ ] Add ARIA labels for accessibility
- [ ] Add colorblind-friendly palette option
- [ ] Write component tests

**Commit:**
```bash
git commit -m "feat(client): add choropleth map visualization component

- Add US states SVG map asset
- Create ChoroplethMap.razor with spending color scale
- Add hover tooltips and click drill-down
- Implement keyboard navigation and ARIA labels
- Add colorblind-friendly palette support

Refs: #054"
```

---

### Phase 9: Location Report Page

**Objective:** Create full report page with map and data table.

**Tasks:**
- [ ] Create `LocationReportPage.razor`
- [ ] Add date range picker (reuse existing component)
- [ ] Integrate choropleth map component
- [ ] Add data table below map with sortable columns
- [ ] Add category filter option
- [ ] Implement CSV export
- [ ] Add navigation from main reports menu
- [ ] Write E2E tests for report page

**Commit:**
```bash
git commit -m "feat(client): add spending by location report page

- Create LocationReportPage.razor with full layout
- Integrate ChoroplethMap component
- Add date range and category filters
- Add sortable data table with region breakdown
- Implement CSV export functionality

Refs: #054"
```

---

### Phase 10: Settings & Privacy Controls

**Objective:** Add settings UI and bulk delete capability.

**Tasks:**
- [ ] Add location settings section to Settings page
- [ ] Add toggle for `EnableLocationData`
- [ ] Add "Delete All Location Data" button with confirmation
- [ ] Create DELETE `/settings/location-data` endpoint
- [ ] Show location feature count/stats before delete
- [ ] Write integration tests for bulk delete

**Commit:**
```bash
git commit -m "feat: add location data settings and privacy controls

- Add location settings section to Settings page
- Add EnableLocationData toggle with immediate effect
- Add bulk delete location data with confirmation
- Add DELETE /settings/location-data endpoint

Refs: #054"
```

---

### Phase 11: Import Integration (Optional Enhancement)

**Objective:** Auto-parse locations during CSV import.

**Tasks:**
- [ ] Integrate location parser into import pipeline
- [ ] Show parsed locations in import preview
- [ ] Allow users to accept/reject parsed locations
- [ ] Track parse success rate in import summary
- [ ] Update import documentation

**Commit:**
```bash
git commit -m "feat(import): add location parsing to CSV import

- Integrate LocationParserService into import pipeline
- Show parsed locations in import preview with confidence
- Allow accept/reject per transaction
- Display parse success rate in import summary

Refs: #054"
```

---

## Out of Scope
- Real-time location tracking or background collection
- Paid or commercial geocoding/location APIs (e.g., Google Maps)
- International maps beyond US states (future enhancement)
- Location-based budgeting rules or alerts
- Merchant location database synchronization

## Dependencies
- SVG map assets (MIT licensed) - must acquire before Phase 8
- Nominatim API availability (public, no key required)
- Browser Geolocation API support (widely available)

## Risks & Mitigations

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Nominatim rate limits exceeded | Medium | Low | Implement caching, batch requests, consider self-hosted instance |
| Location parsing accuracy issues | High | Medium | Start with high-confidence patterns, allow manual correction |
| SVG map performance on mobile | Low | Medium | Optimize SVG, consider canvas fallback |
| GDPR/privacy concerns | Medium | High | Default off, clear disclosure, easy deletion |

## Success Metrics
- 50%+ of imported transactions have parsed location data
- Location report page load time < 2 seconds
- Accessibility audit passes WCAG 2.1 AA
- Zero location data leaks or privacy incidents

---
