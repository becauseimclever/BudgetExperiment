/**
 * Swipe Detection JavaScript Module
 * Handles horizontal swipe gestures for navigation (e.g., month switching).
 * Uses touch events with passive listeners for optimal scroll performance.
 * Budget Experiment - Mobile Experience Feature
 */

/**
 * Initialize swipe detection on an element.
 * @param {HTMLElement} element - The container element to detect swipes on
 * @param {object} dotNetRef - Blazor .NET object reference for callbacks
 * @param {object} options - Configuration options
 * @returns {object} Object with dispose() method for cleanup
 */
export function initSwipeDetection(element, dotNetRef, options = {}) {
    const config = {
        threshold: options.threshold || 50,       // Minimum horizontal distance (px)
        maxTime: options.maxTime || 500,           // Maximum swipe duration (ms)
        velocityThreshold: options.velocityThreshold || 0.3, // px/ms - fast swipes bypass threshold
    };

    let startX = 0;
    let startY = 0;
    let startTime = 0;
    let tracking = false;
    let swiping = false;

    /**
     * Handle touchstart — record starting position.
     * @param {TouchEvent} e
     */
    function onTouchStart(e) {
        // Ignore multi-touch
        if (e.touches.length > 1) {
            tracking = false;
            return;
        }

        startX = e.touches[0].clientX;
        startY = e.touches[0].clientY;
        startTime = Date.now();
        tracking = true;
        swiping = false;
    }

    /**
     * Handle touchmove — apply visual feedback if swiping horizontally.
     * @param {TouchEvent} e
     */
    function onTouchMove(e) {
        if (!tracking) return;

        const currentX = e.touches[0].clientX;
        const currentY = e.touches[0].clientY;
        const deltaX = currentX - startX;
        const deltaY = currentY - startY;

        // If vertical movement dominates early, stop tracking (user is scrolling)
        if (!swiping && Math.abs(deltaY) > Math.abs(deltaX) && Math.abs(deltaY) > 10) {
            tracking = false;
            resetTransform(element);
            return;
        }

        // If horizontal dominates, mark as swiping for visual feedback
        if (Math.abs(deltaX) > 10 && Math.abs(deltaX) > Math.abs(deltaY)) {
            swiping = true;
        }

        // Apply subtle parallax during horizontal swipe
        if (swiping) {
            const dampedX = deltaX * 0.3; // Dampen movement for subtle effect
            requestAnimationFrame(() => {
                element.style.transform = `translateX(${dampedX}px)`;
                element.style.transition = 'none';
            });
        }
    }

    /**
     * Handle touchend — evaluate if swipe meets threshold.
     * @param {TouchEvent} e
     */
    function onTouchEnd(e) {
        if (!tracking) {
            resetTransform(element);
            return;
        }

        tracking = false;

        const endX = e.changedTouches[0].clientX;
        const endY = e.changedTouches[0].clientY;
        const deltaX = endX - startX;
        const deltaY = endY - startY;
        const elapsed = Date.now() - startTime;
        const velocity = Math.abs(deltaX) / elapsed;

        // Reset visual feedback with smooth transition
        resetTransform(element);

        // Determine if this qualifies as a horizontal swipe:
        // 1. Horizontal distance exceeds threshold (or velocity is high enough)
        // 2. Horizontal movement > vertical movement
        // 3. Completed within max time
        const meetsThreshold = Math.abs(deltaX) > config.threshold || velocity > config.velocityThreshold;
        const isHorizontal = Math.abs(deltaX) > Math.abs(deltaY);
        const inTime = elapsed < config.maxTime;

        if (meetsThreshold && isHorizontal && inTime) {
            const direction = deltaX > 0 ? 'OnSwipeRight' : 'OnSwipeLeft';
            try {
                dotNetRef.invokeMethodAsync(direction);
            } catch {
                // DotNet reference may be disposed
            }
        }
    }

    /**
     * Handle touchcancel — clean up tracking state.
     */
    function onTouchCancel() {
        tracking = false;
        swiping = false;
        resetTransform(element);
    }

    /**
     * Reset the element transform with a smooth spring-back animation.
     * @param {HTMLElement} el
     */
    function resetTransform(el) {
        requestAnimationFrame(() => {
            el.style.transition = 'transform 200ms ease-out';
            el.style.transform = '';
            // Clean up inline styles after transition
            setTimeout(() => {
                el.style.transition = '';
                el.style.transform = '';
            }, 210);
        });
    }

    // Attach listeners — passive for touchstart/touchmove to not block scrolling
    element.addEventListener('touchstart', onTouchStart, { passive: true });
    element.addEventListener('touchmove', onTouchMove, { passive: true });
    element.addEventListener('touchend', onTouchEnd, { passive: true });
    element.addEventListener('touchcancel', onTouchCancel, { passive: true });

    return {
        dispose() {
            element.removeEventListener('touchstart', onTouchStart);
            element.removeEventListener('touchmove', onTouchMove);
            element.removeEventListener('touchend', onTouchEnd);
            element.removeEventListener('touchcancel', onTouchCancel);
            resetTransform(element);
        },
    };
}
