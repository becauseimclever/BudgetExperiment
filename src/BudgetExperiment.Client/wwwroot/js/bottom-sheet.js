/**
 * Bottom Sheet JavaScript Module
 * Handles touch gestures for drag-to-resize and swipe-down-to-close
 * Budget Experiment - Mobile Experience Feature
 */

/**
 * Initialize bottom sheet touch interactions
 * @param {HTMLElement} sheetElement - The bottom sheet container element
 * @param {HTMLElement} handleElement - The drag handle element
 * @param {object} dotNetRef - Blazor .NET object reference for callbacks
 * @param {object} options - Configuration options
 * @returns {object} Cleanup function to remove event listeners
 */
export function initBottomSheet(sheetElement, handleElement, dotNetRef, options = {}) {
    const config = {
        closeThreshold: options.closeThreshold || 100, // px to swipe down to close
        velocityThreshold: options.velocityThreshold || 0.5, // px/ms for fast swipe
        minHeight: options.minHeight || 0.2, // 20% minimum height
        maxHeight: options.maxHeight || 0.9, // 90% maximum height
        isDraggable: options.isDraggable !== false,
        isCloseOnSwipeDown: options.isCloseOnSwipeDown !== false,
    };

    let isDragging = false;
    let startY = 0;
    let startHeight = 0;
    let startTime = 0;
    let currentY = 0;

    /**
     * Get the current viewport height
     */
    function getViewportHeight() {
        // Use visualViewport if available (accounts for keyboard on mobile)
        return window.visualViewport?.height || window.innerHeight;
    }

    /**
     * Handle touch/mouse start
     */
    function onPointerDown(e) {
        if (!config.isDraggable) return;

        // Only handle primary touch/click
        if (e.touches && e.touches.length > 1) return;

        isDragging = true;
        startY = e.touches ? e.touches[0].clientY : e.clientY;
        startHeight = sheetElement.getBoundingClientRect().height;
        startTime = Date.now();

        sheetElement.classList.add('is-dragging');

        // Prevent text selection during drag
        document.body.style.userSelect = 'none';
        document.body.style.webkitUserSelect = 'none';
    }

    /**
     * Handle touch/mouse move
     */
    function onPointerMove(e) {
        if (!isDragging) return;

        currentY = e.touches ? e.touches[0].clientY : e.clientY;
        const deltaY = currentY - startY;
        const viewportHeight = getViewportHeight();

        // Calculate new height (dragging down = positive delta = less height)
        let newHeight = startHeight - deltaY;

        // Clamp to min/max bounds
        const minPx = viewportHeight * config.minHeight;
        const maxPx = viewportHeight * config.maxHeight;
        newHeight = Math.max(minPx, Math.min(maxPx, newHeight));

        // Apply the new height directly for smooth dragging
        sheetElement.style.height = `${newHeight}px`;

        // If dragging past the minimum (closing gesture), translate instead
        if (startHeight - deltaY < minPx) {
            const translateY = Math.max(0, deltaY - (startHeight - minPx));
            sheetElement.style.transform = `translateY(${translateY}px)`;
        } else {
            sheetElement.style.transform = '';
        }
    }

    /**
     * Handle touch/mouse end
     */
    function onPointerUp(e) {
        if (!isDragging) return;

        isDragging = false;
        sheetElement.classList.remove('is-dragging');

        // Restore text selection
        document.body.style.userSelect = '';
        document.body.style.webkitUserSelect = '';

        const endY = e.changedTouches ? e.changedTouches[0].clientY : e.clientY;
        const deltaY = endY - startY;
        const elapsed = Date.now() - startTime;
        const velocity = deltaY / elapsed; // px/ms

        // Check for close gesture (swipe down)
        if (config.isCloseOnSwipeDown) {
            const shouldClose =
                deltaY > config.closeThreshold ||
                (velocity > config.velocityThreshold && deltaY > 50);

            if (shouldClose) {
                // Reset inline styles and trigger close
                sheetElement.style.height = '';
                sheetElement.style.transform = '';
                dotNetRef.invokeMethodAsync('OnSwipeClose');
                return;
            }
        }

        // Snap to nearest height breakpoint or restore original
        sheetElement.style.height = '';
        sheetElement.style.transform = '';
    }

    /**
     * Handle touch cancel (e.g., incoming call)
     */
    function onPointerCancel() {
        if (!isDragging) return;

        isDragging = false;
        sheetElement.classList.remove('is-dragging');
        document.body.style.userSelect = '';
        document.body.style.webkitUserSelect = '';

        // Reset to original state
        sheetElement.style.height = '';
        sheetElement.style.transform = '';
    }

    // Attach event listeners to handle element
    if (handleElement) {
        handleElement.addEventListener('touchstart', onPointerDown, { passive: true });
        handleElement.addEventListener('mousedown', onPointerDown);
    }

    // Move and end events on document to catch gestures outside element
    document.addEventListener('touchmove', onPointerMove, { passive: false });
    document.addEventListener('mousemove', onPointerMove);
    document.addEventListener('touchend', onPointerUp, { passive: true });
    document.addEventListener('mouseup', onPointerUp);
    document.addEventListener('touchcancel', onPointerCancel, { passive: true });

    // Return cleanup function
    return {
        dispose: function () {
            if (handleElement) {
                handleElement.removeEventListener('touchstart', onPointerDown);
                handleElement.removeEventListener('mousedown', onPointerDown);
            }
            document.removeEventListener('touchmove', onPointerMove);
            document.removeEventListener('mousemove', onPointerMove);
            document.removeEventListener('touchend', onPointerUp);
            document.removeEventListener('mouseup', onPointerUp);
            document.removeEventListener('touchcancel', onPointerCancel);
        }
    };
}

/**
 * Trap focus within an element (for accessibility)
 * @param {HTMLElement} containerElement - The container to trap focus within
 * @returns {object} Object with dispose method to remove trap
 */
export function trapFocus(containerElement) {
    const focusableSelector = [
        'a[href]',
        'button:not([disabled])',
        'textarea:not([disabled])',
        'input:not([disabled])',
        'select:not([disabled])',
        '[tabindex]:not([tabindex="-1"])',
    ].join(', ');

    function getFocusableElements() {
        return Array.from(containerElement.querySelectorAll(focusableSelector))
            .filter(el => el.offsetParent !== null); // Only visible elements
    }

    function onKeyDown(e) {
        if (e.key !== 'Tab') return;

        const focusable = getFocusableElements();
        if (focusable.length === 0) return;

        const first = focusable[0];
        const last = focusable[focusable.length - 1];

        if (e.shiftKey && document.activeElement === first) {
            e.preventDefault();
            last.focus();
        } else if (!e.shiftKey && document.activeElement === last) {
            e.preventDefault();
            first.focus();
        }
    }

    containerElement.addEventListener('keydown', onKeyDown);

    // Focus first focusable element
    const focusable = getFocusableElements();
    if (focusable.length > 0) {
        focusable[0].focus();
    }

    return {
        dispose: function () {
            containerElement.removeEventListener('keydown', onKeyDown);
        }
    };
}

/**
 * Trigger haptic feedback if supported
 * @param {string} type - Type of feedback: 'light', 'medium', 'heavy'
 */
export function triggerHapticFeedback(type = 'light') {
    // Check for Vibration API support
    if ('vibrate' in navigator) {
        const durations = {
            light: 10,
            medium: 20,
            heavy: 30,
        };
        navigator.vibrate(durations[type] || 10);
    }
}

/**
 * Prevent body scroll when bottom sheet is open
 * @param {boolean} prevent - Whether to prevent scrolling
 */
export function preventBodyScroll(prevent) {
    if (prevent) {
        document.body.style.overflow = 'hidden';
        document.body.style.position = 'fixed';
        document.body.style.width = '100%';
        document.body.style.top = `-${window.scrollY}px`;
    } else {
        const scrollY = document.body.style.top;
        document.body.style.overflow = '';
        document.body.style.position = '';
        document.body.style.width = '';
        document.body.style.top = '';
        window.scrollTo(0, parseInt(scrollY || '0', 10) * -1);
    }
}
