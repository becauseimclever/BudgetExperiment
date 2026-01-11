# Feature: Design System Overhaul

## Overview
Implement a comprehensive design system with consistent styling, a theme system (light/dark mode), and a modern Fluent-inspired look and feel. The implementation will use pure CSS with CSS custom properties (variables) for theming, built on top of a lightweight utility-first approach.

## Current State Analysis

### Current Issues
1. **Inconsistent Styling**: Mix of inline `<style>` blocks in components and CSS files
2. **No Theme System**: Hardcoded colors throughout components
3. **Multiple CSS Files**: `app.css` and `fluentui-app.css` exist but FluentUI library is not used
4. **Unused Dependencies**: `index.html` references FluentUI CSS files that aren't needed
5. **Component-Scoped Styles**: Each page has its own `<style>` block with duplicated patterns
6. **No Dark Mode**: Only light theme available
7. **Emoji Icons**: Using emojis (📅, 🏦, 💳) instead of proper icon system

### Files with Inline Styles
- [MainLayout.razor](../src/BudgetExperiment.Client/Layout/MainLayout.razor)
- [CalendarLayout.razor](../src/BudgetExperiment.Client/Layout/CalendarLayout.razor)
- [Accounts.razor](../src/BudgetExperiment.Client/Pages/Accounts.razor)
- [AccountTransactions.razor](../src/BudgetExperiment.Client/Pages/AccountTransactions.razor)
- [Calendar.razor](../src/BudgetExperiment.Client/Pages/Calendar.razor)
- [Recurring.razor](../src/BudgetExperiment.Client/Pages/Recurring.razor)
- Plus various components in `/Components/`

---

## User Stories

### US-001: Consistent Visual Design
**As a** user  
**I want** a consistent visual design across all pages  
**So that** the application feels polished and professional

### US-002: Dark Mode Support
**As a** user  
**I want** to switch between light and dark themes  
**So that** I can use the app comfortably in different lighting conditions

### US-003: System Theme Detection
**As a** user  
**I want** the app to automatically use my system's theme preference  
**So that** it matches my other applications by default

### US-004: Theme Persistence
**As a** user  
**I want** my theme preference to be remembered  
**So that** I don't have to change it every time I visit

### US-005: Accessible Color Contrast
**As a** user with visual impairments  
**I want** sufficient color contrast throughout the app  
**So that** I can read and interact with all content

### US-006: Responsive Design
**As a** user  
**I want** the app to work well on mobile, tablet, and desktop  
**So that** I can manage my budget from any device

---

## Design System Architecture

### CSS Framework Choice: Custom CSS with CSS Variables

**Why not Bootstrap?**
- Heavy (even minimal build is ~25KB+ CSS)
- Opinionated class naming that doesn't match Fluent Design
- Would require extensive customization anyway

**Why not Tailwind CSS?**
- Requires build process (PostCSS)
- Utility classes in markup can be verbose
- Learning curve for team

**Chosen Approach: Custom Design System**
- Lightweight (~10KB minified)
- CSS custom properties for theming
- Fluent Design-inspired components
- Full control over aesthetics
- No build dependencies

### File Structure

```
wwwroot/css/
├── design-system/
│   ├── tokens.css          # CSS custom properties (colors, spacing, typography)
│   ├── reset.css           # Modern CSS reset/normalize
│   ├── base.css            # Base element styles
│   ├── layout.css          # Layout utilities (grid, flex, spacing)
│   ├── components/
│   │   ├── buttons.css     # Button styles
│   │   ├── cards.css       # Card/panel styles
│   │   ├── forms.css       # Form controls
│   │   ├── tables.css      # Table styles
│   │   ├── modals.css      # Modal/dialog styles
│   │   ├── navigation.css  # Nav menu styles
│   │   └── alerts.css      # Alert/notification styles
│   └── utilities.css       # Utility classes (text, colors, spacing)
├── themes/
│   ├── light.css           # Light theme token overrides
│   ├── dark.css            # Dark theme token overrides
│   └── vscode-dark.css     # VS Code Dark theme token overrides
└── app.css                 # Main entry point (imports all)
```

---

## Design Tokens (CSS Custom Properties)

### Color Palette

```css
:root {
  /* Brand Colors */
  --color-brand-primary: #0078d4;
  --color-brand-primary-hover: #106ebe;
  --color-brand-primary-active: #005a9e;
  
  /* Semantic Colors */
  --color-success: #107c10;
  --color-warning: #ffb900;
  --color-error: #d13438;
  --color-info: #0078d4;
  
  /* Neutral Colors (Light Theme) */
  --color-background: #faf9f8;
  --color-surface: #ffffff;
  --color-surface-secondary: #f3f2f1;
  --color-border: #d2d0ce;
  --color-border-subtle: #edebe9;
  
  /* Text Colors */
  --color-text-primary: #323130;
  --color-text-secondary: #605e5c;
  --color-text-disabled: #a19f9d;
  --color-text-inverse: #ffffff;
  
  /* Transaction-Specific */
  --color-income: #107c10;
  --color-expense: #d13438;
  --color-transfer: #0078d4;
  --color-recurring: #8764b8;
}
```

### Dark Theme Overrides

```css
[data-theme="dark"] {
  --color-background: #1f1f1f;
  --color-surface: #292929;
  --color-surface-secondary: #333333;
  --color-border: #484644;
  --color-border-subtle: #3b3a39;
  
  --color-text-primary: #f3f2f1;
  --color-text-secondary: #b3b0ad;
  --color-text-disabled: #797775;
  
  --color-brand-primary: #4ba0e8;
  --color-brand-primary-hover: #5caff5;
  
  --color-income: #6ccb5f;
  --color-expense: #f87c7c;
}
```

### VS Code Dark Theme

Inspired by VS Code's default dark theme with its signature blue accents and darker backgrounds.

```css
[data-theme="vscode-dark"] {
  /* Backgrounds - VS Code's signature dark grays */
  --color-background: #1e1e1e;
  --color-surface: #252526;
  --color-surface-secondary: #2d2d2d;
  --color-border: #3c3c3c;
  --color-border-subtle: #333333;
  
  /* Text */
  --color-text-primary: #cccccc;
  --color-text-secondary: #9d9d9d;
  --color-text-disabled: #6b6b6b;
  --color-text-inverse: #ffffff;
  
  /* Brand - VS Code blue */
  --color-brand-primary: #0e639c;
  --color-brand-primary-hover: #1177bb;
  --color-brand-primary-active: #094771;
  
  /* Semantic Colors - VS Code palette */
  --color-success: #4ec9b0;   /* VS Code teal/cyan */
  --color-warning: #dcdcaa;   /* VS Code yellow */
  --color-error: #f14c4c;     /* VS Code red */
  --color-info: #3794ff;      /* VS Code bright blue */
  
  /* Transaction-Specific */
  --color-income: #4ec9b0;    /* Teal for positive */
  --color-expense: #f14c4c;   /* Red for negative */
  --color-transfer: #569cd6;  /* VS Code blue */
  --color-recurring: #c586c0; /* VS Code purple/magenta */
  
  /* Shadows - Deeper for dark theme */
  --shadow-sm: 0 1px 2px rgba(0, 0, 0, 0.3);
  --shadow-md: 0 2px 4px rgba(0, 0, 0, 0.4);
  --shadow-lg: 0 4px 8px rgba(0, 0, 0, 0.5);
  --shadow-xl: 0 8px 16px rgba(0, 0, 0, 0.6);
  
  /* Focus ring - VS Code blue */
  --focus-ring: 0 0 0 1px var(--color-background), 
                0 0 0 3px #007fd4;
  
  /* Accent colors for syntax-highlighting-like UI elements */
  --color-accent-blue: #569cd6;
  --color-accent-green: #6a9955;
  --color-accent-orange: #ce9178;
  --color-accent-purple: #c586c0;
  --color-accent-yellow: #dcdcaa;
}
```

### Typography

```css
:root {
  /* Font Family */
  --font-family-base: 'Segoe UI', system-ui, -apple-system, sans-serif;
  --font-family-mono: 'Cascadia Code', 'Consolas', monospace;
  
  /* Font Sizes */
  --font-size-xs: 0.75rem;    /* 12px */
  --font-size-sm: 0.875rem;   /* 14px */
  --font-size-base: 1rem;     /* 16px */
  --font-size-lg: 1.125rem;   /* 18px */
  --font-size-xl: 1.25rem;    /* 20px */
  --font-size-2xl: 1.5rem;    /* 24px */
  --font-size-3xl: 2rem;      /* 32px */
  
  /* Font Weights */
  --font-weight-normal: 400;
  --font-weight-medium: 500;
  --font-weight-semibold: 600;
  --font-weight-bold: 700;
  
  /* Line Heights */
  --line-height-tight: 1.25;
  --line-height-base: 1.5;
  --line-height-relaxed: 1.75;
}
```

### Spacing

```css
:root {
  /* Spacing Scale */
  --space-1: 0.25rem;   /* 4px */
  --space-2: 0.5rem;    /* 8px */
  --space-3: 0.75rem;   /* 12px */
  --space-4: 1rem;      /* 16px */
  --space-5: 1.25rem;   /* 20px */
  --space-6: 1.5rem;    /* 24px */
  --space-8: 2rem;      /* 32px */
  --space-10: 2.5rem;   /* 40px */
  --space-12: 3rem;     /* 48px */
  
  /* Component-specific */
  --radius-sm: 2px;
  --radius-md: 4px;
  --radius-lg: 8px;
  --radius-xl: 12px;
  --radius-full: 9999px;
}
```

### Shadows & Effects

```css
:root {
  /* Elevation Shadows */
  --shadow-sm: 0 1px 2px rgba(0, 0, 0, 0.05);
  --shadow-md: 0 2px 4px rgba(0, 0, 0, 0.1);
  --shadow-lg: 0 4px 8px rgba(0, 0, 0, 0.12);
  --shadow-xl: 0 8px 16px rgba(0, 0, 0, 0.14);
  
  /* Transitions */
  --transition-fast: 150ms ease;
  --transition-base: 200ms ease;
  --transition-slow: 300ms ease;
  
  /* Focus Ring */
  --focus-ring: 0 0 0 2px var(--color-background), 
                0 0 0 4px var(--color-brand-primary);
}

[data-theme="dark"] {
  --shadow-sm: 0 1px 2px rgba(0, 0, 0, 0.2);
  --shadow-md: 0 2px 4px rgba(0, 0, 0, 0.3);
  --shadow-lg: 0 4px 8px rgba(0, 0, 0, 0.4);
  --shadow-xl: 0 8px 16px rgba(0, 0, 0, 0.5);
}
```

---

## Component Specifications

### Buttons

```css
.btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: var(--space-2);
  padding: var(--space-2) var(--space-4);
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  line-height: var(--line-height-tight);
  border-radius: var(--radius-md);
  border: 1px solid transparent;
  cursor: pointer;
  transition: all var(--transition-fast);
}

.btn:focus-visible {
  outline: none;
  box-shadow: var(--focus-ring);
}

.btn-primary {
  background: var(--color-brand-primary);
  color: var(--color-text-inverse);
}

.btn-primary:hover {
  background: var(--color-brand-primary-hover);
}

.btn-secondary {
  background: var(--color-surface);
  color: var(--color-text-primary);
  border-color: var(--color-border);
}

.btn-danger {
  background: var(--color-error);
  color: var(--color-text-inverse);
}

.btn-ghost {
  background: transparent;
  color: var(--color-text-primary);
}

.btn-ghost:hover {
  background: var(--color-surface-secondary);
}
```

### Cards

```css
.card {
  background: var(--color-surface);
  border: 1px solid var(--color-border-subtle);
  border-radius: var(--radius-lg);
  box-shadow: var(--shadow-sm);
}

.card-header {
  padding: var(--space-4);
  border-bottom: 1px solid var(--color-border-subtle);
}

.card-body {
  padding: var(--space-4);
}

.card-footer {
  padding: var(--space-4);
  border-top: 1px solid var(--color-border-subtle);
}
```

### Form Controls

```css
.form-control {
  width: 100%;
  padding: var(--space-2) var(--space-3);
  font-size: var(--font-size-sm);
  line-height: var(--line-height-base);
  color: var(--color-text-primary);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  transition: border-color var(--transition-fast),
              box-shadow var(--transition-fast);
}

.form-control:hover {
  border-color: var(--color-text-secondary);
}

.form-control:focus {
  outline: none;
  border-color: var(--color-brand-primary);
  box-shadow: 0 0 0 1px var(--color-brand-primary);
}

.form-control.invalid {
  border-color: var(--color-error);
}

.form-label {
  display: block;
  margin-bottom: var(--space-1);
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--color-text-primary);
}
```

### Tables

```css
.table {
  width: 100%;
  border-collapse: collapse;
  background: var(--color-surface);
}

.table th,
.table td {
  padding: var(--space-3) var(--space-4);
  text-align: left;
  border-bottom: 1px solid var(--color-border-subtle);
}

.table th {
  font-weight: var(--font-weight-semibold);
  color: var(--color-text-secondary);
  background: var(--color-surface-secondary);
}

.table tbody tr:hover {
  background: var(--color-surface-secondary);
}

/* Amount column styling */
.table .amount-positive {
  color: var(--color-income);
  font-weight: var(--font-weight-medium);
}

.table .amount-negative {
  color: var(--color-expense);
  font-weight: var(--font-weight-medium);
}
```

### Navigation

```css
.nav-sidebar {
  background: var(--color-surface);
  border-right: 1px solid var(--color-border-subtle);
}

.nav-item {
  display: flex;
  align-items: center;
  gap: var(--space-3);
  padding: var(--space-3) var(--space-4);
  color: var(--color-text-primary);
  text-decoration: none;
  transition: background var(--transition-fast);
}

.nav-item:hover {
  background: var(--color-surface-secondary);
}

.nav-item.active {
  background: var(--color-brand-primary);
  color: var(--color-text-inverse);
}

.nav-item .nav-icon {
  width: 20px;
  height: 20px;
  flex-shrink: 0;
}
```

---

## Icon System

### Approach: SVG Icons

Replace emoji icons with consistent SVG icons. Options:

1. **Inline SVG** (Recommended for small icon set)
   - No external dependencies
   - Themeable with CSS
   - ~20 icons needed

2. **Icon Component**
   ```razor
   <Icon Name="calendar" Size="20" />
   ```

### Required Icons

| Icon | Usage |
|------|-------|
| calendar | Calendar page nav |
| refresh | Recurring page nav |
| arrows-horizontal | Transfers page nav |
| building-bank | Accounts page nav |
| credit-card | Account sub-items |
| plus | Add buttons |
| pencil | Edit buttons |
| trash | Delete buttons |
| chevron-down | Dropdowns |
| chevron-right | Collapsed nav |
| x | Close/dismiss |
| check | Success states |
| alert-triangle | Warnings |
| info | Info states |
| sun | Light theme |
| moon | Dark theme |
| code | VS Code theme |
| computer | System theme |

---

## Theme Implementation

### Theme Service

```csharp
public sealed class ThemeService
{
    private const string StorageKey = "budget-theme";
    
    public event Action? OnThemeChanged;
    
    public ThemeMode CurrentTheme { get; private set; } = ThemeMode.System;
    
    public async Task InitializeAsync(IJSRuntime js)
    {
        var stored = await js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
        if (Enum.TryParse<ThemeMode>(stored, out var theme))
        {
            CurrentTheme = theme;
        }
        await ApplyThemeAsync(js);
    }
    
    public async Task SetThemeAsync(IJSRuntime js, ThemeMode theme)
    {
        CurrentTheme = theme;
        await js.InvokeVoidAsync("localStorage.setItem", StorageKey, theme.ToString());
        await ApplyThemeAsync(js);
        OnThemeChanged?.Invoke();
    }
    
    private async Task ApplyThemeAsync(IJSRuntime js)
    {
        var effectiveTheme = CurrentTheme == ThemeMode.System
            ? await GetSystemThemeAsync(js)
            : CurrentTheme.ToString().ToLower();
        
        await js.InvokeVoidAsync("document.documentElement.setAttribute", "data-theme", effectiveTheme);
    }
    
    private async Task<string> GetSystemThemeAsync(IJSRuntime js)
    {
        var prefersDark = await js.InvokeAsync<bool>(
            "window.matchMedia('(prefers-color-scheme: dark)').matches");
        return prefersDark ? "dark" : "light";
    }
}

public enum ThemeMode
{
    Light,
    Dark,
    VSCodeDark,
    System
}
```

### Theme Toggle Component

```razor
<div class="theme-toggle">
    <button class="@GetButtonClass(ThemeMode.Light)" @onclick="() => SetTheme(ThemeMode.Light)">
        <Icon Name="sun" /> Light
    </button>
    <button class="@GetButtonClass(ThemeMode.Dark)" @onclick="() => SetTheme(ThemeMode.Dark)">
        <Icon Name="moon" /> Dark
    </button>
    <button class="@GetButtonClass(ThemeMode.VSCodeDark)" @onclick="() => SetTheme(ThemeMode.VSCodeDark)">
        <Icon Name="code" /> VS Code
    </button>
    <button class="@GetButtonClass(ThemeMode.System)" @onclick="() => SetTheme(ThemeMode.System)">
        <Icon Name="computer" /> System
    </button>
</div>
```

### CSS Implementation

```css
/* System preference detection */
@media (prefers-color-scheme: dark) {
  :root:not([data-theme="light"]) {
    /* Dark theme variables */
  }
}

/* Explicit theme classes */
[data-theme="dark"] {
  /* Dark theme variables */
}

[data-theme="light"] {
  /* Light theme variables (default, can be empty) */
}
```

---

## Implementation Plan

### Phase 1: Design Tokens & Base Styles
1. [x] Create `wwwroot/css/design-system/` directory structure
2. [x] Create `tokens.css` with all CSS custom properties
3. [x] Create `reset.css` with modern CSS reset
4. [x] Create `base.css` with base element styles
5. [x] Create light and dark theme files
6. [ ] Update `index.html` to load new CSS structure

### Phase 2: Component Styles ✅
1. [x] Create `buttons.css` with button variants
2. [x] Create `cards.css` with card styles
3. [x] Create `forms.css` with form control styles
4. [x] Create `tables.css` with table styles
5. [x] Create `modals.css` with modal/dialog styles
6. [x] Create `navigation.css` with nav styles
7. [x] Create `alerts.css` with alert/notification styles
8. [x] Create `utilities.css` with utility classes

### Phase 3: Layout System ✅
1. [x] Create `layout.css` with grid and flex utilities
2. [x] Define responsive breakpoints
3. [x] Create spacing utilities
4. [x] Create container classes

### Phase 4: Icon System ✅
1. [x] Create `Icon.razor` component
2. [x] Add SVG icons as embedded resources or inline
3. [x] Replace all emoji icons with Icon component
4. [x] Create `icons.css` for icon styling
5. [ ] Add icon documentation

### Phase 5: Theme System
1. [ ] Create `ThemeService.cs`
2. [ ] Create `ThemeToggle.razor` component
3. [ ] Add theme toggle to header/settings
4. [ ] Implement localStorage persistence
5. [ ] Add system theme detection
6. [ ] Test theme transitions

### Phase 6: Component Migration
1. [ ] Migrate `MainLayout.razor` - remove inline styles
2. [ ] Migrate `NavMenu.razor` - remove inline styles
3. [ ] Migrate `Accounts.razor` - use design system classes
4. [ ] Migrate `AccountTransactions.razor` - use design system classes
5. [ ] Migrate `Calendar.razor` - use design system classes
6. [ ] Migrate `Recurring.razor` - use design system classes
7. [ ] Migrate all components in `/Components/`

### Phase 0: Cleanup Unused Dependencies ✅
1. [x] Remove `fluentui-app.css` from `wwwroot/css/`
2. [x] Remove FluentUI CSS references from `index.html`:
   - `css/fluentui-app.css`
   - `_content/Microsoft.FluentUI.AspNetCore.Components/css/reboot.css`
   - `_content/Microsoft.FluentUI.AspNetCore.Components/css/fluent.css`
   - `_content/Microsoft.FluentUI.AspNetCore.Components/css/fluent-icons.css`
3. [x] Remove `Microsoft.FluentUI.AspNetCore.Components` NuGet package if installed (was not installed)
4. [x] Verify application still runs correctly

### Phase 7: Polish & Documentation
1. [ ] Review all pages for consistency
2. [ ] Test responsive behavior on all screen sizes
3. [ ] Test dark mode on all pages
4. [ ] Verify WCAG AA color contrast
5. [ ] Create style guide documentation
6. [ ] Remove old `app.css` after migration complete

---

## Responsive Breakpoints

```css
:root {
  /* Breakpoint values (for reference in media queries) */
  --breakpoint-sm: 640px;
  --breakpoint-md: 768px;
  --breakpoint-lg: 1024px;
  --breakpoint-xl: 1280px;
}

/* Mobile first approach */
/* Base styles for mobile */

@media (min-width: 640px) {
  /* Small tablets and up */
}

@media (min-width: 768px) {
  /* Tablets and up */
}

@media (min-width: 1024px) {
  /* Desktops and up */
}

@media (min-width: 1280px) {
  /* Large desktops */
}
```

---

## Accessibility Considerations

### Color Contrast
- All text must meet WCAG AA standard (4.5:1 for normal text, 3:1 for large text)
- Interactive elements must have 3:1 contrast against background
- Don't rely solely on color to convey information

### Focus States
- All interactive elements must have visible focus indicator
- Focus ring uses `--focus-ring` variable for consistency
- Tab order must be logical

### Motion
- Respect `prefers-reduced-motion` preference
- Provide option to disable animations

```css
@media (prefers-reduced-motion: reduce) {
  *,
  *::before,
  *::after {
    animation-duration: 0.01ms !important;
    animation-iteration-count: 1 !important;
    transition-duration: 0.01ms !important;
  }
}
```

---

## Migration Strategy

### Approach: Gradual Migration

1. **Add new design system alongside existing styles**
   - New CSS files don't conflict with existing
   - Can migrate page by page

2. **Migrate layout first**
   - `MainLayout.razor` sets the foundation
   - Navigation affects all pages

3. **Migrate shared components**
   - Buttons, forms, modals used everywhere
   - One migration benefits all pages

4. **Migrate pages individually**
   - Lower risk than big-bang approach
   - Can review each page thoroughly

5. **Remove old CSS last**
   - Only after all pages migrated
   - Verify nothing breaks

### CSS Class Naming Convention

Use BEM-like naming for clarity:

```css
/* Block */
.card { }

/* Element */
.card__header { }
.card__body { }
.card__footer { }

/* Modifier */
.card--elevated { }
.card--compact { }

/* State */
.card.is-loading { }
.card.is-selected { }
```

---

## Future Enhancements

- [ ] Custom theme builder (choose your own brand color)
- [ ] High contrast theme for accessibility
- [ ] Compact/comfortable density toggle
- [ ] Animation/motion preferences
- [ ] Print stylesheet
- [ ] CSS-only charts/visualizations
- [ ] Component playground/storybook-like documentation
