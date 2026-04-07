# Feature 141: Settings — Kakeibo/Kaizen Preferences

> **Status:** Planned

## Prerequisites

- Feature 129b (Feature Flag Implementation) must be merged before implementation begins.
- Feature 131 (KakeiboCategory on BudgetCategory) should be completed before user settings are deployed.
- Feature 134 (Calendar Enhancements — Spending Heatmap) should be completed.
- Feature 135 (Monthly Reflection) should be completed.
- Feature 136 (Kaizen Micro-Goals) should be completed.

---

## Feature Flag

**Flag Name:** None  
**Note:** Settings page itself is not feature-flagged. The individual preferences control feature visibility on a per-user basis. Server-side feature flags (129b) control whether the feature exists at all; user settings control whether the individual user sees it.

---

## Overview

A new section on the `/settings` page — "Kakeibo & Kaizen Preferences" — allows users to control the visibility and behavior of Kakeibo and Kaizen features. These are user preferences that enable/disable certain UI elements and prompts, independent of the backend feature flag system.

The settings provide granular control:
- Show/hide the spending heatmap on the calendar
- Enable/disable monthly reflection prompts
- Enable/disable weekly micro-goals feature
- Show/hide Kakeibo category badges on calendar day cells

These settings are stored per user and returned in the settings API response.

---

## Domain Model Changes

**Entity:** `UserSettings`  
**New Fields** (all `bool` with specified defaults):

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `ShowSpendingHeatmap` | `bool` | `true` | Controls visibility of the calendar heatmap overlay (green/amber/red intensity) |
| `ShowMonthlyReflectionPrompts` | `bool` | `true` | Enable/disable the month-start intention modal and month-end reflection modal |
| `EnableKaizenMicroGoals` | `bool` | `true` | Enable/disable the weekly micro-goal feature (visibility in calendar and forms) |
| `ShowKakeiboCalendarBadges` | `bool` | `true` | Enable/disable Kakeibo category badges on calendar day cells |

**Migration:**
```sql
ALTER TABLE "UserSettings" ADD COLUMN "ShowSpendingHeatmap" BOOLEAN NOT NULL DEFAULT TRUE;
ALTER TABLE "UserSettings" ADD COLUMN "ShowMonthlyReflectionPrompts" BOOLEAN NOT NULL DEFAULT TRUE;
ALTER TABLE "UserSettings" ADD COLUMN "EnableKaizenMicroGoals" BOOLEAN NOT NULL DEFAULT TRUE;
ALTER TABLE "UserSettings" ADD COLUMN "ShowKakeiboCalendarBadges" BOOLEAN NOT NULL DEFAULT TRUE;
```

**EF Core Configuration:**
```csharp
modelBuilder.Entity<UserSettings>()
    .Property(u => u.ShowSpendingHeatmap)
    .HasDefaultValue(true);

modelBuilder.Entity<UserSettings>()
    .Property(u => u.ShowMonthlyReflectionPrompts)
    .HasDefaultValue(true);

modelBuilder.Entity<UserSettings>()
    .Property(u => u.EnableKaizenMicroGoals)
    .HasDefaultValue(true);

modelBuilder.Entity<UserSettings>()
    .Property(u => u.ShowKakeiboCalendarBadges)
    .HasDefaultValue(true);
```

---

## API Changes

**Existing Endpoints (Modified):**
```
GET /api/v1/settings
PUT /api/v1/settings
```

**Modified Response/Request DTO:**

```csharp
public class UserSettingsDto
{
    // Existing fields ...
    
    // NEW FIELDS:
    public bool ShowSpendingHeatmap { get; set; } = true;
    public bool ShowMonthlyReflectionPrompts { get; set; } = true;
    public bool EnableKaizenMicroGoals { get; set; } = true;
    public bool ShowKakeiboCalendarBadges { get; set; } = true;
}
```

**No new endpoints.** The existing `/settings` endpoint is extended with new fields.

---

## UI Changes

**Modified Component:** Settings page (e.g., `SettingsView.razor`)

**New Section:** "Kakeibo & Kaizen Preferences"

**UI Elements:**
1. **Spending Heatmap Toggle**
   - Label: "Show Spending Heatmap"
   - Sublabel: "Display daily spending intensity (green/amber/red) on the calendar"
   - Toggle switch bound to `ShowSpendingHeatmap`

2. **Monthly Reflection Prompts Toggle**
   - Label: "Show Monthly Reflection Prompts"
   - Sublabel: "Prompt at month-start for intentions and month-end for reflections"
   - Toggle switch bound to `ShowMonthlyReflectionPrompts`

3. **Kaizen Micro-Goals Toggle**
   - Label: "Enable Kaizen Micro-Goals"
   - Sublabel: "Track weekly micro-improvements and see outcomes on the calendar"
   - Toggle switch bound to `EnableKaizenMicroGoals`

4. **Kakeibo Calendar Badges Toggle**
   - Label: "Show Kakeibo Badges on Calendar"
   - Sublabel: "Display Kakeibo category indicators on calendar day cells"
   - Toggle switch bound to `ShowKakeiboCalendarBadges`

**Interaction:**
- User changes a toggle
- Component immediately updates the local state
- Component calls `PUT /api/v1/settings` with the modified `UserSettingsDto`
- On success, show a brief confirmation toast or similar feedback
- On error, revert the toggle and show error message

---

## Acceptance Criteria

- [ ] `UserSettings` entity has four new boolean fields with correct defaults (all true)
- [ ] Migration adds columns with proper default values
- [ ] `UserSettingsDto` includes the four new fields
- [ ] `GET /api/v1/settings` returns the new fields in the response
- [ ] `PUT /api/v1/settings` accepts and persists the new fields
- [ ] Settings page displays "Kakeibo & Kaizen Preferences" section with four toggles
- [ ] Each toggle is labeled clearly with description text
- [ ] Toggling a switch updates component state and calls `PUT /api/v1/settings`
- [ ] Success feedback (toast/message) is shown after save
- [ ] Error feedback is shown if the API call fails, with toggle reverted
- [ ] All existing settings functionality remains unchanged
- [ ] All unit and integration tests pass; OpenAPI spec is updated

---

## Implementation Notes

- **Persistence:** All four fields are persisted to the database via the `UserSettings` entity. No client-side-only state — the source of truth is the database.
- **Defaults:** New users get all four settings enabled (true) by default. This ensures the full Kakeibo + Kaizen experience is "on" out of the box, but users can opt out of specific features.
- **Feature Flag Coordination:** The backend feature flags (129b) control whether a feature exists at all (e.g., `Features:Kaizen:Dashboard` controls whether the Kaizen Dashboard report is accessible). User settings (this feature) control whether the user *sees* the feature they've enabled. Example flow:
  - Admin disables `Features:Calendar:SpendingHeatmap` via feature flag → heatmap endpoint/logic is disabled for all users, regardless of their settings
  - User toggles `ShowSpendingHeatmap: false` in settings → they personally don't see the heatmap, but the feature is still available to other users
- **Cascade Effects:** When a user disables a feature (e.g., `EnableKaizenMicroGoals: false`), the calendar should not show micro-goal UI or attempt to fetch goal data. The client checks this flag before rendering dependent components.
- **Future Extensibility:** Design the settings section as a reusable component so additional preferences can be added without restructuring.

