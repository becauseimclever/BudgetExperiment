# Theming Guide

This guide explains how the Budget Experiment theming system works and how to add new themes.

## Overview

The application uses a CSS variable-based theming system that allows switching between multiple visual themes. The system consists of:

1. **CSS Variables** - Defined in theme files (`wwwroot/css/themes/`)
2. **ThemeService** - Blazor service for managing theme state
3. **theme.js** - JavaScript module for theme persistence and application
4. **ThemeToggle** - UI component for theme selection

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│  ThemeToggle.razor (UI Component)                               │
│    ├── Displays available themes from ThemeService              │
│    └── Calls ThemeService.SetThemeAsync() on selection          │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  ThemeService.cs (Blazor Service)                               │
│    ├── AvailableThemes: List of ThemeOption records             │
│    ├── CurrentTheme: Currently selected theme                   │
│    ├── SetThemeAsync(): Updates theme via JS interop            │
│    └── ThemeChanged event for component updates                 │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  theme.js (JavaScript Module)                                   │
│    ├── setTheme(): Persists to localStorage                     │
│    ├── applyTheme(): Sets data-theme attribute on <html>        │
│    └── updateMetaThemeColor(): Updates mobile browser chrome    │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  CSS Theme Files (e.g., dark.css, monopoly.css)                 │
│    └── [data-theme="themename"] { CSS variables }               │
└─────────────────────────────────────────────────────────────────┘
```

## Adding a New Theme

Follow these steps to add a new theme to the application:

### Step 1: Create the Theme CSS File

Create a new CSS file in `src/BudgetExperiment.Client/wwwroot/css/themes/`.

**File:** `mytheme.css`

```css
/* ==========================================================================
   My Theme
   Budget Experiment Design System
   
   Brief description of the theme aesthetic.
   ========================================================================== */

[data-theme="mytheme"] {
  /* Backgrounds */
  --color-background: #ffffff;
  --color-surface: #ffffff;
  --color-surface-secondary: #f5f5f5;
  --color-border: #e0e0e0;
  --color-border-subtle: #f0f0f0;

  /* App Shell Colors */
  --color-bg-secondary: #f5f5f5;
  --color-bg-elevated: #ffffff;
  --color-header-bg: #333333;
  --color-header-text: #ffffff;
  --color-header-text-hover: #cccccc;
  --color-header-hover: rgba(255, 255, 255, 0.1);
  --color-sidebar-bg: #f5f5f5;
  --color-sidebar-text: #333333;
  --color-sidebar-text-hover: #000000;

  /* Text Colors */
  --color-text-primary: #1a1a1a;
  --color-text-secondary: #666666;
  --color-text-disabled: #999999;
  --color-text-inverse: #ffffff;

  /* Brand Colors */
  --color-brand-primary: #0066cc;
  --color-brand-primary-hover: #0055aa;
  --color-brand-primary-active: #004488;
  --color-brand-rgb: 0, 102, 204;

  /* Semantic Colors */
  --color-success: #28a745;
  --color-warning: #ffc107;
  --color-warning-rgb: 255, 193, 7;
  --color-error: #dc3545;
  --color-info: #17a2b8;

  /* Warning alert colors */
  --color-warning-bg: #fff3cd;
  --color-warning-border: #ffc107;
  --color-warning-text: #856404;

  /* Transaction-Specific */
  --color-income: #28a745;
  --color-expense: #dc3545;
  --color-transfer: #17a2b8;
  --color-recurring: #6f42c1;
  --color-recurring-rgb: 111, 66, 193;

  /* Shadows */
  --shadow-sm: 0 1px 2px rgba(0, 0, 0, 0.1);
  --shadow-md: 0 2px 4px rgba(0, 0, 0, 0.15);
  --shadow-lg: 0 4px 8px rgba(0, 0, 0, 0.2);
  --shadow-xl: 0 8px 16px rgba(0, 0, 0, 0.25);

  /* Focus ring */
  --focus-ring: 0 0 0 2px var(--color-background),
                0 0 0 4px var(--color-brand-primary);

  /* Color scheme for native elements */
  color-scheme: light; /* or 'dark' for dark themes */

  /* Optional: Custom font stack */
  --font-family-base: 'Your Font', system-ui, sans-serif;
}

/* Optional: Theme-specific overrides */
[data-theme="mytheme"] .some-component {
  /* Custom styles */
}
```

### Step 2: Import the Theme in app.css

Add the import in `src/BudgetExperiment.Client/wwwroot/css/app.css`:

```css
/* Theme Variants */
@import "themes/light.css";
@import "themes/dark.css";
@import "themes/vscode-dark.css";
@import "themes/monopoly.css";
@import "themes/mytheme.css";  /* Add your theme */
```

### Step 3: Register the Theme in ThemeService

Add the theme to `AvailableThemes` in `src/BudgetExperiment.Client/Services/ThemeService.cs`:

```csharp
public static IReadOnlyList<ThemeOption> AvailableThemes { get; } = new List<ThemeOption>
{
    new("system", "System", "monitor"),
    new("light", "Light", "sun"),
    new("dark", "Dark", "moon"),
    new("vscode-dark", "VS Code", "code"),
    new("monopoly", "Monopoly", "dice"),
    new("mytheme", "My Theme", "icon-name"),  // Add your theme
};
```

**Available icons:** The icon should be one of the Lucide icons available in the app. Common choices:
- `sun`, `moon` - light/dark themes
- `monitor`, `laptop` - system/computer themes
- `code` - developer themes
- `palette`, `paintbrush` - artistic themes
- `sparkles`, `star` - fun/flashy themes
- `dice`, `gamepad-2` - game-inspired themes

### Step 4: Add Meta Theme Color in theme.js

Add the theme's header color to the colors map in `src/BudgetExperiment.Client/wwwroot/js/theme.js`:

```javascript
const colors = {
    'light': '#ffffff',
    'dark': '#1a1a2e',
    'vscode-dark': '#1e1e1e',
    'monopoly': '#c1e4da',
    'mytheme': '#333333',  // Your theme's header-bg color
};
```

This color is used for the mobile browser's address bar (meta theme-color).

### Step 5: Add Themed Icons (Optional)

Themes can have custom icon sets that replace the default Lucide icons. This is useful for creating immersive themed experiences (e.g., pixelated icons for Windows 95, hand-drawn icons for Crayon Box).

#### Option A: Add Icons to ThemedIconRegistry

Add your theme's icon mappings in `src/BudgetExperiment.Client/Services/ThemedIconRegistry.cs`:

```csharp
private static readonly FrozenDictionary<string, FrozenDictionary<string, string>> IconSets =
    new Dictionary<string, FrozenDictionary<string, string>>
    {
        // ... existing themes ...
        
        ["mytheme"] = new Dictionary<string, string>
        {
            ["calendar"] = "mytheme-calendar",
            ["home"] = "mytheme-home",
            ["settings"] = "mytheme-settings",
            // Add mappings for icons you want to customize
        }.ToFrozenDictionary(),
    }.ToFrozenDictionary();
```

#### Option B: Add Custom Icon Paths

Add the SVG paths for your themed icons in `src/BudgetExperiment.Client/Components/Common/Icon.razor`:

```csharp
// In the switch statement, add your themed icon paths:
"mytheme-calendar" => "<your-svg-path-here />",
"mytheme-home" => "<your-svg-path-here />",
```

**Tips for themed icons:**
- Use thicker `stroke-width` (2.5-3) for bold/retro styles
- Add `fill="currentColor"` for solid fills
- Keep within the 24x24 viewBox
- Test visibility at small sizes (16-20px)

#### Common Icons to Theme

These icons appear in the navbar and should be prioritized:

| Icon Name | Used For |
|-----------|----------|
| `calendar` | Calendar page |
| `refresh` | Recurring bills |
| `repeat` | Auto-transfers |
| `check-circle` | Reconciliation |
| `arrows-horizontal` | Transfers |
| `calculator` | Paycheck planner |
| `tag` | Categories |
| `filter` | Auto-categorize |
| `pie-chart` | Budget/Reports |
| `bar-chart` | Reports section |
| `bank` | Accounts |
| `upload` | Import |
| `settings` | Settings |
| `sparkles` | AI features |
| `plus` | Add buttons |

### Step 6: Verify Theme Visibility

After adding the theme:

1. Run the application
2. Open the theme dropdown in the header
3. Select your new theme
4. Verify:
   - The dropdown button is visible in the header
   - The dropdown menu items are readable
   - All page elements have proper contrast
   - Forms, buttons, and cards look correct

## Required CSS Variables

These variables **must** be defined for proper theme functionality:

| Variable | Purpose |
|----------|---------|
| `--color-background` | Page/app background |
| `--color-surface` | Card/container backgrounds |
| `--color-surface-secondary` | Alternate surface color |
| `--color-border` | Standard borders |
| `--color-border-subtle` | Subtle/light borders |
| `--color-header-bg` | Top navigation background |
| `--color-header-text` | Top navigation text |
| `--color-sidebar-bg` | Sidebar background |
| `--color-sidebar-text` | Sidebar link text |
| `--color-text-primary` | Primary body text |
| `--color-text-secondary` | Secondary/muted text |
| `--color-brand-primary` | Primary action/link color |
| `--color-success` | Success state color |
| `--color-warning` | Warning state color |
| `--color-error` | Error state color |
| `--color-income` | Income transaction color |
| `--color-expense` | Expense transaction color |

## Available Themes

| Theme | Value | Description |
|-------|-------|-------------|
| System | `system` | Follows OS light/dark preference |
| Light | `light` | Clean light theme |
| Dark | `dark` | Modern dark theme |
| Accessible | `accessible` | High-contrast WCAG 2.0 AA compliant theme |
| VS Code | `vscode-dark` | VS Code dark editor style |
| Monopoly | `monopoly` | Board game-inspired parchment and teal |
| Windows 95 | `win95` | Classic 90s Windows with 3D bevels |
| macOS | `macos` | Apple-inspired clean aesthetic |
| GeoCities | `geocities` | Nostalgic 90s web with neon colors |
| Crayon Box | `crayons` | Playful Crayola-inspired primary colors |

### Accessible Theme

The Accessible theme is designed for users with low vision or visual impairments. It provides:

- **High contrast ratios**: All text meets WCAG 2.0 AAA (7:1+) contrast
- **Solid borders**: 2px black borders on interactive elements
- **Enhanced focus**: 3px focus ring with white offset
- **Underlined links**: Links always underlined for non-color identification
- **Larger text**: Slightly larger font sizes for readability

The theme auto-applies when the system detects:
- Windows High Contrast Mode (`forced-colors: active`)
- User prefers more contrast (`prefers-contrast: more`)

See [ACCESSIBILITY.md](./ACCESSIBILITY.md) for more details.

## Tips for Theme Design

1. **Contrast**: Ensure text has sufficient contrast against backgrounds (WCAG AA minimum)
2. **Semantic Colors**: Keep success=green, error=red, warning=yellow conventions
3. **Header Visibility**: The theme dropdown must be visible in the header
4. **Color Scheme**: Set `color-scheme: light` or `dark` for native form elements
5. **Test Everything**: Check forms, tables, modals, and all interactive elements

## Future Enhancements

- UI theme builder for custom user themes
- User-specific theme preferences stored in database
- Theme export/import functionality
