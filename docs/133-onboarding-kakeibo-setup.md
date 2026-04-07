# Feature 133: Onboarding — Kakeibo Setup Step

> **Status:** Planned

## Prerequisites

Feature 129b (Feature Flag Implementation) must be merged before implementation begins.

Feature 131 (Budget Categories — Kakeibo Category Routing) must be merged before implementation begins.

## Overview

This feature extends the existing 4-step onboarding wizard to a 5-step flow, adding a dedicated **Kakeibo Setup** step immediately after budget configuration. The step introduces the four Kakeibo categories (Essentials, Wants, Culture, Unexpected) with clear explanations and examples, asks the user to confirm or correct their existing expense categories' Kakeibo routing, and optionally introduces the monthly reflection ritual concept.

This step is also triggered on first login after a database migration that adds `BudgetCategory.KakeiboCategory`, ensuring existing users who set up the app before Kakeibo shipped get the wizard experience automatically.

## Problem Statement

### Current State

- The existing 4-step onboarding covers account setup, initial transaction entry, and budget goals.
- New Kakeibo philosophy is not introduced during onboarding — existing users never learn the four categories.
- Categories created before Feature 131 (or by users during onboarding) get migration default routing (Wants) without user awareness or confirmation.
- Users have no guided introduction to *why* category routing matters or what the Kakeibo categories mean.

### Target State

- Onboarding is 5 steps: Accounts, Transactions, Budgets, **Kakeibo Setup** (NEW), Goals.
- Step 5 (Kakeibo Setup) explains the four Kakeibo categories with examples and asks the user to assign their expense categories.
- Users see existing categories (from Step 1 or already in their account) and confirm or change their Kakeibo routing.
- A brief, optional introduction to monthly reflection ("At the start of each month, we'll ask what you want to save") teases Feature 135.
- Flag `UserSettings.HasCompletedKakeiboSetup` is set to `true` at the end of the step, preventing re-display.
- Existing users who log in post-migration get the wizard on next login if `HasCompletedKakeiboSetup == false`.

## Domain Model Changes

### UserSettings Extension

Add a flag to `UserSettings` (already exists; adding new property):

```csharp
public class UserSettings
{
    // ... existing fields ...
    
    /// <summary>
    /// Whether the user has completed the Kakeibo category setup (either during onboarding or post-migration).
    /// </summary>
    public bool HasCompletedKakeiboSetup { get; set; } = false;
}
```

### Database Migration

**Schema change:**

```sql
ALTER TABLE "UserSettings" ADD COLUMN "HasCompletedKakeiboSetup" boolean NOT NULL DEFAULT false;
```

> **Note:** After Feature 131's migration (which adds `KakeiboCategory` to `BudgetCategories`), this migration can run. New users will have `HasCompletedKakeiboSetup = false` and enter onboarding normally. Existing users will also have it set to `false`, triggering the wizard on next login.

## API Changes

### Onboarding Progress Endpoint

**Existing endpoint:** `GET /api/v1/users/onboarding-status` (or equivalent)

**Response enhancement:**

```csharp
public class OnboardingStatusResponse
{
    public int CompletedSteps { get; set; }  // 0-4 (existing)
    public int TotalSteps { get; set; }      // Now 5 (was 4)
    public bool HasCompletedKakeiboSetup { get; set; }  // NEW
}
```

### Onboarding Save Endpoint

**Existing endpoint (likely):** `POST /api/v1/users/onboarding` or per-step saves

**For Kakeibo step, assume existing service can persist `UserSettings.HasCompletedKakeiboSetup`:**

- Request: Mark Kakeibo step complete
- Response: Returns updated onboarding status
- Side effect: Sets `HasCompletedKakeiboSetup = true`

**If per-step endpoint:**

```csharp
public class CompleteKakeiboSetupRequest
{
    // No request body needed — just a completion marker
    // Category edits happen via existing category update endpoint
}
```

Response:

```csharp
public class OnboardingStepResponse
{
    public int StepNumber { get; set; }
    public string StepName { get; set; }
    public bool IsComplete { get; set; }
}
```

### Get Onboarding Data

**Endpoint:** `GET /api/v1/users/onboarding-data` (or extend existing)

Returns:

```csharp
public class OnboardingDataResponse
{
    public List<AccountResponse> Accounts { get; set; }
    public List<BudgetCategoryResponse> ExpenseCategories { get; set; }  // NEW: for Kakeibo step
    public List<BudgetGoalResponse> BudgetGoals { get; set; }
}
```

The Kakeibo step fetches expense categories and their current `KakeiboCategory` routing from this endpoint.

## UI Changes

### Onboarding Wizard Extension

**Component:** `src/BudgetExperiment.Client/Components/Onboarding/OnboardingWizard.razor`

**Changes:**

- Update step counter: "Step 5 of 5" (was "Step 4 of 4").
- Add fifth step component: `KakeiboSetupStep.razor` (NEW).
- Wizard navigation: "Next" button on Step 4 advances to Step 5; Step 5 "Complete" button finalizes onboarding.
- Persist completion state: on Step 5 completion, call backend to set `HasCompletedKakeiboSetup = true`.

### New Component: KakeiboSetupStep.razor

**Path:** `src/BudgetExperiment.Client/Components/Onboarding/KakeiboSetupStep.razor`

**Structure:**

1. **Header Section:**
   - Title: "How you spend your money"
   - Subtitle: "Let's categorize your spending into four buckets. This helps you see where your money goes in a more meaningful way."

2. **Kakeibo Category Explainer:**
   - Display four cards, one for each Kakeibo category:
     - **Essentials** (必要) — "Things you need to live: groceries, utilities, housing, transportation."
     - **Wants** (欲しい) — "Things you enjoy but don't need: dining, entertainment, hobbies, travel."
     - **Culture** (文化) — "Things that enrich your mind and spirit: education, books, charity, museums."
     - **Unexpected** (予期しない) — "Things you didn't plan for: emergency repairs, surprise medical bills."
   - Each card has a distinct icon and subtle background colour.

3. **Category Assignment Section:**
   - Heading: "Your expense categories"
   - List all existing **Expense** categories (loaded from `GET /api/v1/users/onboarding-data`).
   - For each category, show:
     - Category name
     - Dropdown menu to select its Kakeibo bucket
     - If a category has been assigned already (from migration or prior setup), show its current value selected.
   - Grouped presentation option: group categories by their assigned bucket (e.g., "Essentials: Groceries, Utilities, ...").

4. **Optional: Monthly Reflection Teaser**
   - Text: "At the start of each month, you'll be asked: 'What do you want to save?' This helps you set an intention and reflect on your progress."
   - CTA: "Learn more" (optional link to future Feature 135 docs or a help page).

5. **Action Buttons:**
   - "Back" button (navigate to Step 4)
   - "Complete Onboarding" button (saves all category assignments and sets `HasCompletedKakeiboSetup = true`)
   - Optional: "Skip for now" button that still marks setup as complete but doesn't force users to assign all categories (permissive UX).

**Interaction flow:**

1. Component loads categories via API call to `GET /api/v1/users/onboarding-data`.
2. Populate dropdowns with category names and their current Kakeibo routing.
3. User changes dropdowns as needed.
4. On "Complete Onboarding," call each changed category's `PUT /api/v1/categories/{id}` endpoint to persist the new routing.
5. After all categories are saved, POST to `/api/v1/users/onboarding` (or similar) to mark step complete.
6. Navigate to onboarding success page or home.

**State management:**

- Track `initialCategoryRoutings` (fetched state).
- Track `changedCategories` (dirty state).
- Only send PUT requests for categories that changed.

### Post-Migration Trigger (Existing User)

**In `App.razor` or layout initialization:**

```csharp
@code {
    private async Task OnInitializedAsync()
    {
        var userSettings = await UserService.GetSettingsAsync();
        if (!userSettings.HasCompletedKakeiboSetup)
        {
            // Redirect to onboarding Kakeibo step
            Navigation.NavigateTo("/onboarding?step=kakeibo");
        }
    }
}
```

**Onboarding wizard detects `?step=kakeibo` query param and shows only the Kakeibo step** (skip the first 4 steps if they're already complete).

Alternatively: show a **modal** prompting "We've added a new way to understand your spending. Set it up now?" with options "Set up now" (redirect to onboarding step 5) or "Later" (dismisses modal, can be re-triggered from settings).

## Feature Flag

**None.** Kakeibo onboarding is always on once Feature 131 ships. There is no toggle.

## Acceptance Criteria

- [ ] `UserSettings.HasCompletedKakeiboSetup` field added (nullable bool, default false).
- [ ] Database migration adds the column with correct default.
- [ ] Onboarding step counter updated to 5 (frontend and API).
- [ ] `OnboardingStatusResponse` includes `HasCompletedKakeiboSetup` property.
- [ ] `GET /api/v1/users/onboarding-data` endpoint returns list of Expense categories with their Kakeibo routing.
- [ ] `KakeiboSetupStep.razor` component displays four Kakeibo categories with clear explanations and icons.
- [ ] Component loads existing expense categories and populates Kakeibo assignment dropdowns.
- [ ] User can change any category's Kakeibo routing via dropdown.
- [ ] "Complete Onboarding" button calls PUT endpoint for each changed category and marks setup complete.
- [ ] Completion sets `UserSettings.HasCompletedKakeiboSetup = true`.
- [ ] Existing users log in post-migration and see onboarding trigger (modal or redirect) if `HasCompletedKakeiboSetup` is false.
- [ ] Post-migration trigger allows user to set up now or dismiss (dismissal marks as complete in a permissive way, or re-triggers on next login).
- [ ] Monthly reflection teaser text is shown (optional but recommended).
- [ ] Skip/Later options are permissive (don't block users from using the app).
- [ ] All existing onboarding tests pass; new tests for Kakeibo step logic and API integration.

## Implementation Order

1. **Add `HasCompletedKakeiboSetup` to `UserSettings` entity** (Domain).
2. **Create database migration** with schema change.
3. **Update onboarding API endpoints** to expose expense categories and persist completion status.
4. **Create `KakeiboSetupStep.razor` component** (Blazor UI).
5. **Integrate step into `OnboardingWizard.razor`** (update step counter, navigation, routing).
6. **Implement post-migration trigger** in `App.razor` or layout (check flag and redirect if needed).
7. **Add query param support to onboarding wizard** to jump to a specific step (e.g., `?step=kakeibo`).
8. **Add tests** for API endpoints, component initialization, category fetching, dropdown changes, and completion persistence.
9. **Test existing user flow:** migrate database, log in, see onboarding trigger.

**Dependencies:** Feature 131 (categories must have Kakeibo routing) and Feature 129b (optional, for consistency).

**Related work:** Feature 131 includes a one-time **Kakeibo Setup Wizard** accessible from category settings and on first login post-migration. Feature 133 integrates this wizard into the standard onboarding flow for new users. Both use the same component or coordinate state.
