// Theme management JavaScript module
const STORAGE_KEY = 'budget-experiment-theme';
const THEME_ATTRIBUTE = 'data-theme';

/**
 * Gets the saved theme from localStorage.
 * @returns {string|null} The saved theme or null.
 */
export function getTheme() {
    return localStorage.getItem(STORAGE_KEY);
}

/**
 * Sets and applies a theme.
 * @param {string} theme - The theme to set (light, dark, vscode-dark, system).
 */
export function setTheme(theme) {
    localStorage.setItem(STORAGE_KEY, theme);
    applyTheme(theme);
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

// Apply theme on initial load (before Blazor initializes)
(function initTheme() {
    const savedTheme = getTheme() || 'system';
    applyTheme(savedTheme);
})();
