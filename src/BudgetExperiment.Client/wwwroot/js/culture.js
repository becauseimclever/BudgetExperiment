/**
 * Browser culture detection for localization support.
 * Returns the browser's preferred language and timezone.
 */
export function detectCulture() {
    return {
        language: navigator.language || 'en-US',
        timeZone: Intl.DateTimeFormat().resolvedOptions().timeZone || 'UTC'
    };
}
