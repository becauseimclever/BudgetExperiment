# Mobile Experience: Calendar, Quick Add, and AI Assistant
> **Status:** 🗒️ Planning

## Feature Overview

This feature aims to deliver a first-class mobile experience for Budget Experiment users, focusing on:
- A responsive, touch-friendly Calendar view
- Streamlined workflows for adding new transactions on mobile
- Easy, prominent access to the AI Assistant for transaction entry and suggestions

## Motivation

Many users access Budget Experiment from mobile devices. The current UI is optimized for desktop, making some workflows (especially calendar navigation and transaction entry) cumbersome on smaller screens. Improving the mobile experience will:
- Increase daily engagement
- Reduce friction for on-the-go transaction entry
- Leverage AI to further simplify mobile workflows

## Goals

- Responsive Calendar view with touch gestures (swipe, tap, pinch)
- "Quick Add" button always accessible on mobile
- Mobile-optimized transaction entry form (large inputs, minimal steps)
- Persistent, easy-to-reach AI Assistant button (floating or bottom nav)
- AI Assistant can pre-fill or suggest transaction details based on context
- Ensure all new UI meets accessibility and WCAG AA standards

## Non-Goals

- Full offline support (future feature)
- Redesign of desktop experience (focus is mobile-first improvements)

## User Stories

1. **As a mobile user, I want to view and navigate the Calendar easily, so I can see my transactions by date.**
2. **As a mobile user, I want a prominent, always-available button to quickly add a new transaction from anywhere.**
3. **As a mobile user, I want the transaction entry form to be optimized for touch, with large fields and minimal required input.**
4. **As a mobile user, I want to access the AI Assistant with one tap, so I can get help or suggestions when adding a transaction.**
5. **As a mobile user, I want the AI Assistant to pre-fill transaction details when possible, reducing manual entry.**

## Acceptance Criteria

- Calendar view is fully responsive and supports touch navigation
- "Quick Add" button is visible and accessible on all mobile screens
- Transaction entry form adapts to mobile (large touch targets, minimal scrolling)
- AI Assistant button is always visible on mobile (floating action or bottom nav)
- AI Assistant can suggest or auto-fill transaction details
- All new UI passes accessibility checks (WCAG AA)

## Design Notes

- Use Blazor's built-in responsive features; avoid third-party UI libraries
- Consider a floating action button (FAB) for "Quick Add" and AI Assistant
- Test with real devices and emulators for touch/gesture support
- Ensure color contrast, focus states, and ARIA labels for accessibility

## Open Questions

- Should the AI Assistant open as a modal, drawer, or bottom sheet on mobile?
- Should the Calendar support week/month toggle on mobile?
- How should error handling and validation be surfaced on small screens?

---

*Created: 2026-01-26*
*Author: @becauseimclever*
