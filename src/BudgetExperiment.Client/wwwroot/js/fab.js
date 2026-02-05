/**
 * fab.js - Floating Action Button JavaScript Module
 * Budget Experiment Design System
 * Provides haptic feedback support for FAB interactions
 */

/**
 * Triggers haptic feedback if supported by the device.
 * Uses the Vibration API with a short pattern suitable for button feedback.
 */
export function triggerHapticFeedback() {
    // Check if Vibration API is supported
    if ('vibrate' in navigator) {
        // Short vibration pattern for button feedback (10ms)
        navigator.vibrate(10);
    }
}

/**
 * Creates a ripple effect on the target element.
 * @param {HTMLElement} element - The element to add the ripple to.
 * @param {MouseEvent|TouchEvent} event - The interaction event.
 */
export function createRipple(element, event) {
    const ripple = document.createElement('span');
    ripple.classList.add('fab-ripple');

    const rect = element.getBoundingClientRect();
    const size = Math.max(rect.width, rect.height);

    // Get click/touch position
    let x, y;
    if (event.touches && event.touches.length > 0) {
        x = event.touches[0].clientX - rect.left - size / 2;
        y = event.touches[0].clientY - rect.top - size / 2;
    } else {
        x = event.clientX - rect.left - size / 2;
        y = event.clientY - rect.top - size / 2;
    }

    ripple.style.width = ripple.style.height = `${size}px`;
    ripple.style.left = `${x}px`;
    ripple.style.top = `${y}px`;

    element.appendChild(ripple);

    // Remove ripple after animation completes
    ripple.addEventListener('animationend', () => {
        ripple.remove();
    });
}

/**
 * Initializes ripple effects on FAB buttons.
 * @param {HTMLElement} containerElement - The FAB container element.
 */
export function initRippleEffects(containerElement) {
    if (!containerElement) {
        return;
    }

    const buttons = containerElement.querySelectorAll('.fab-primary, .fab-secondary');

    buttons.forEach(button => {
        button.addEventListener('click', (event) => {
            createRipple(button, event);
        });
    });
}
