# Calendar Recurring Transactions Visibility
> **Status:** ✅ Complete

## Feature Overview

Display recurring transactions and reminders as visually distinct elements (icons, highlights) within the calendar view. Users can easily identify and interact with recurring items.

## Motivation

Recurring transactions are a core part of budgeting. Making them visible in the calendar helps users anticipate upcoming expenses/income and manage reminders.

## Goals
- Show recurring transactions/reminders in the calendar
- Use icons, color, or highlights for distinction
- Allow interaction (view/edit details)

## Acceptance Criteria
- [x] Recurring items are visually distinct in the calendar
- [x] Users can view/edit recurring transaction details from the calendar
- [x] UI is accessible and responsive

## Implementation Summary

This feature was implemented as part of earlier calendar development work:

- **CalendarDay.razor**: Displays recurring indicator with refresh icon and projected total when `Day.HasRecurring` is true
- **DayDetail.razor**: Shows dedicated "Scheduled Recurring" section with confirm, edit, and skip actions for each recurring item
- **Visual distinction**: CSS class `has-recurring` applied to days, icons and color-coded amounts distinguish recurring from actual transactions

---
*Created: 2026-01-26*
*Completed: 2026-02-01*
*Author: @becauseimclever*
