// Theme management JavaScript module
const STORAGE_KEY = 'budget-experiment-theme';
const OVERRIDE_KEY = 'budget-experiment-theme-override';
const THEME_ATTRIBUTE = 'data-theme';

// Track if accessible theme was auto-applied
let accessibilityPreferenceDetected = false;
let themeAutoApplied = false;

/**
 * Gets the saved theme from localStorage.
 * @returns {string|null} The saved theme or null.
 */
export function getTheme() {
    return localStorage.getItem(STORAGE_KEY);
}

/**
 * Detects if user has accessibility preferences enabled.
 * Checks for Windows High Contrast Mode and increased contrast preference.
 * @returns {boolean} True if accessibility theme should be auto-applied.
 */
export function detectAccessibilityPreferences() {
    // Windows High Contrast Mode
    if (window.matchMedia('(forced-colors: active)').matches) {
        return true;
    }
    // User prefers more contrast
    if (window.matchMedia('(prefers-contrast: more)').matches) {
        return true;
    }
    return false;
}

/**
 * Checks if user has explicitly overridden the theme.
 * @returns {boolean} True if user has an explicit override.
 */
export function hasExplicitOverride() {
    return localStorage.getItem(OVERRIDE_KEY) === 'true';
}

/**
 * Gets the effective theme considering accessibility preferences.
 * @returns {string} Theme to apply.
 */
export function getEffectiveTheme() {
    const savedTheme = getTheme();
    const hasOverride = hasExplicitOverride();
    
    // If user explicitly chose a theme, respect it
    if (hasOverride && savedTheme) {
        accessibilityPreferenceDetected = detectAccessibilityPreferences();
        themeAutoApplied = false;
        return savedTheme;
    }
    
    // Auto-apply accessible theme if preferences detected
    if (detectAccessibilityPreferences()) {
        accessibilityPreferenceDetected = true;
        themeAutoApplied = true;
        return 'accessible';
    }
    
    accessibilityPreferenceDetected = false;
    themeAutoApplied = false;
    
    // Fall back to saved or system theme
    return savedTheme || 'system';
}

/**
 * Sets and applies a theme with explicit override flag.
 * @param {string} theme - The theme to set.
 * @param {boolean} isExplicitChoice - Whether user explicitly chose this theme.
 */
export function setTheme(theme, isExplicitChoice = true) {
    localStorage.setItem(STORAGE_KEY, theme);
    
    // Mark as explicit override if user made a choice
    if (isExplicitChoice) {
        localStorage.setItem(OVERRIDE_KEY, 'true');
        themeAutoApplied = false;
    }
    
    applyTheme(theme);
}

/**
 * Clears the explicit theme override, allowing auto-detection to work.
 */
export function clearThemeOverride() {
    localStorage.removeItem(OVERRIDE_KEY);
    const effectiveTheme = getEffectiveTheme();
    applyTheme(effectiveTheme);
}

/**
 * Applies a theme to the document.
 * @param {string} theme - The theme to apply.
 */
export function applyTheme(theme) {
    const resolvedTheme = resolveTheme(theme);
    document.documentElement.setAttribute(THEME_ATTRIBUTE, resolvedTheme);
    
    // Update meta theme-color for mobile browsers
    updateMetaThemeColor(resolvedTheme);
}

/**
 * Resolves 'system' theme to actual light/dark based on user preference.
 * @param {string} theme - The theme to resolve.
 * @returns {string} The resolved theme name.
 */
function resolveTheme(theme) {
    if (theme === 'system') {
        return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    }
    return theme;
}

/**
 * Gets the resolved theme (for Blazor interop).
 * @returns {string} The resolved theme name.
 */
export function getResolvedTheme() {
    const savedTheme = getTheme() || 'system';
    return resolveTheme(savedTheme);
}

/**
 * Updates the meta theme-color tag for mobile browser UI.
 * @param {string} theme - The resolved theme name.
 */
function updateMetaThemeColor(theme) {
    let metaThemeColor = document.querySelector('meta[name="theme-color"]');
    
    if (!metaThemeColor) {
        metaThemeColor = document.createElement('meta');
        metaThemeColor.name = 'theme-color';
        document.head.appendChild(metaThemeColor);
    }
    
    // Set appropriate color based on theme
    const colors = {
        'light': '#ffffff',
        'dark': '#1a1a2e',
        'vscode-dark': '#1e1e1e',
        'accessible': '#ffffff',
        'monopoly': '#c1e4da',
        'win95': '#000080',
        'macos': '#e8e8ed',
        'geocities': '#ff00ff',
        'crayons': '#1f75fe'
    };
    
    metaThemeColor.content = colors[theme] || colors['light'];
}

// Listen for system theme changes
window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', (e) => {
    const savedTheme = getTheme();
    if (savedTheme === 'system' || !savedTheme) {
        applyTheme('system');
    }
});

// Listen for contrast preference changes
window.matchMedia('(prefers-contrast: more)').addEventListener('change', () => {
    if (!hasExplicitOverride()) {
        const effectiveTheme = getEffectiveTheme();
        applyTheme(effectiveTheme);
    }
});

// Listen for forced-colors changes (Windows High Contrast Mode)
window.matchMedia('(forced-colors: active)').addEventListener('change', () => {
    if (!hasExplicitOverride()) {
        const effectiveTheme = getEffectiveTheme();
        applyTheme(effectiveTheme);
    }
});

/**
 * Gets the accessibility state for Blazor interop.
 * @returns {object} Object with accessibility detection state.
 */
export function getAccessibilityState() {
    return {
        isAccessibilityPreferenceDetected: accessibilityPreferenceDetected,
        wasThemeAutoApplied: themeAutoApplied,
        hasExplicitOverride: hasExplicitOverride()
    };
}

// Apply theme on initial load (before Blazor initializes)
(function initTheme() {
    const effectiveTheme = getEffectiveTheme();
    applyTheme(effectiveTheme);
})();
