export function initializeGridLayout(gridElement, dotNetHelper, options) {
    if (!gridElement || !dotNetHelper) {
        return;
    }

    const state = {
        gridElement,
        dotNetHelper,
        options: options || {},
        active: null,
        lastLayoutKey: null,
        onPointerDown: null,
        onPointerMove: null,
        onPointerUp: null,
        onKeyDown: null
    };

    const getBreakpoint = () => {
        const width = window.innerWidth || 0;
        if (width <= (state.options.breakpointSmMax || 720)) {
            return 'sm';
        }
        if (width <= (state.options.breakpointMdMax || 1024)) {
            return 'md';
        }
        return 'lg';
    };

    const readMetrics = () => {
        const styles = getComputedStyle(state.gridElement);
        const columns = parseInt(styles.getPropertyValue('--grid-columns'), 10) || 12;
        const rowHeight = parseInt(styles.getPropertyValue('--grid-row-height'), 10) || state.options.rowHeight || 24;
        const gap = parseInt(styles.getPropertyValue('--grid-gap'), 10) || state.options.gap || 12;
        const rect = state.gridElement.getBoundingClientRect();
        const totalGap = gap * Math.max(0, columns - 1);
        const columnWidth = columns > 0 ? (rect.width - totalGap) / columns : rect.width;

        return {
            columns,
            rowHeight,
            gap,
            rect,
            columnWidth,
            columnUnit: columnWidth + gap,
            rowUnit: rowHeight + gap
        };
    };

    const clamp = (value, min, max) => Math.max(min, Math.min(max, value));

    const toDatasetKey = (breakpoint) => {
        if (!breakpoint) {
            return 'Lg';
        }

        return breakpoint.charAt(0).toUpperCase() + breakpoint.slice(1).toLowerCase();
    };

    const readLayout = (widget, breakpoint) => {
        const prefix = toDatasetKey(breakpoint);
        const x = parseInt(widget.dataset[`colStart${prefix}`], 10) || 1;
        const y = parseInt(widget.dataset[`rowStart${prefix}`], 10) || 1;
        const w = parseInt(widget.dataset[`colSpan${prefix}`], 10) || 1;
        const h = parseInt(widget.dataset[`rowSpan${prefix}`], 10) || 1;

        return { x, y, w, h };
    };

    const readConstraints = (widget) => {
        return {
            minW: parseInt(widget.dataset.minW, 10) || 1,
            minH: parseInt(widget.dataset.minH, 10) || 1,
            maxW: parseInt(widget.dataset.maxW, 10) || 12,
            maxH: parseInt(widget.dataset.maxH, 10) || 12
        };
    };

    const applyLayoutData = (widget, breakpoint, layout) => {
        const prefix = toDatasetKey(breakpoint);
        widget.dataset[`colStart${prefix}`] = layout.x;
        widget.dataset[`rowStart${prefix}`] = layout.y;
        widget.dataset[`colSpan${prefix}`] = layout.w;
        widget.dataset[`rowSpan${prefix}`] = layout.h;
    };

    const notifyLayout = (widgetId, breakpoint, layout) => {
        const key = `${widgetId}-${breakpoint}-${layout.x}-${layout.y}-${layout.w}-${layout.h}`;
        if (state.lastLayoutKey === key) {
            return;
        }

        state.lastLayoutKey = key;
        state.dotNetHelper.invokeMethodAsync(
            'UpdateWidgetLayoutAsync',
            widgetId,
            breakpoint,
            layout.x,
            layout.y,
            layout.w,
            layout.h);
    };

    const snapDrag = (start, delta, metrics, constraints) => {
        const width = clamp(start.w, constraints.minW, Math.min(constraints.maxW, metrics.columns));
        const height = clamp(start.h, constraints.minH, constraints.maxH);

        const offsetX = start.left + delta.x;
        const offsetY = start.top + delta.y;

        const x = clamp(Math.round(offsetX / metrics.columnUnit) + 1, 1, Math.max(1, metrics.columns - width + 1));
        const y = Math.max(1, Math.round(offsetY / metrics.rowUnit) + 1);

        return { x, y, w: width, h: height };
    };

    const snapResize = (start, delta, metrics, constraints) => {
        const targetWidth = start.width + delta.x;
        const targetHeight = start.height + delta.y;

        const width = clamp(
            Math.round((targetWidth + metrics.gap) / metrics.columnUnit),
            constraints.minW,
            Math.min(constraints.maxW, metrics.columns));

        const height = clamp(
            Math.round((targetHeight + metrics.gap) / metrics.rowUnit),
            constraints.minH,
            constraints.maxH);

        const x = clamp(start.x, 1, Math.max(1, metrics.columns - width + 1));
        const y = Math.max(1, start.y);

        return { x, y, w: width, h: height };
    };

    const startInteraction = (event, widget, mode) => {
        if (event.button !== 0) {
            return;
        }

        const breakpoint = getBreakpoint();
        const metrics = readMetrics();
        const constraints = readConstraints(widget);
        const rect = widget.getBoundingClientRect();
        const gridRect = state.gridElement.getBoundingClientRect();
        const layout = readLayout(widget, breakpoint);

        state.active = {
            widget,
            mode,
            breakpoint,
            metrics,
            constraints,
            startX: event.clientX,
            startY: event.clientY,
            startLayout: layout,
            startLeft: rect.left - gridRect.left,
            startTop: rect.top - gridRect.top,
            startWidth: rect.width,
            startHeight: rect.height
        };

        widget.classList.add('is-dragging');
        state.gridElement.classList.add('is-dragging');
        event.preventDefault();
    };

    const updateInteraction = (event) => {
        if (!state.active) {
            return;
        }

        const delta = {
            x: event.clientX - state.active.startX,
            y: event.clientY - state.active.startY
        };

        let layout;
        if (state.active.mode === 'resize') {
            layout = snapResize(
                {
                    x: state.active.startLayout.x,
                    y: state.active.startLayout.y,
                    width: state.active.startWidth,
                    height: state.active.startHeight
                },
                delta,
                state.active.metrics,
                state.active.constraints);
        } else {
            layout = snapDrag(
                {
                    x: state.active.startLayout.x,
                    y: state.active.startLayout.y,
                    w: state.active.startLayout.w,
                    h: state.active.startLayout.h,
                    left: state.active.startLeft,
                    top: state.active.startTop
                },
                delta,
                state.active.metrics,
                state.active.constraints);
        }

        applyLayoutData(state.active.widget, state.active.breakpoint, layout);
        notifyLayout(state.active.widget.dataset.widgetId, state.active.breakpoint, layout);
        event.preventDefault();
    };

    const endInteraction = () => {
        if (!state.active) {
            return;
        }

        state.active.widget.classList.remove('is-dragging');
        state.gridElement.classList.remove('is-dragging');
        state.active = null;
    };

    state.onPointerDown = (event) => {
        const header = event.target.closest('.report-widget-header');
        const resizeHandle = event.target.closest('.report-widget-resize-handle');
        const widget = event.target.closest('.report-widget');

        if (!widget) {
            return;
        }

        if (resizeHandle) {
            startInteraction(event, widget, 'resize');
            return;
        }

        if (header) {
            startInteraction(event, widget, 'drag');
        }
    };

    state.onPointerMove = (event) => {
        if (!state.active) {
            return;
        }

        updateInteraction(event);
    };

    state.onPointerUp = () => {
        endInteraction();
    };

    state.onKeyDown = (event) => {
        const header = event.target.closest('.report-widget-header');
        const widget = event.target.closest('.report-widget');

        if (!header || !widget) {
            return;
        }

        const key = event.key;
        if (!['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight'].includes(key)) {
            return;
        }

        const breakpoint = getBreakpoint();
        const metrics = readMetrics();
        const constraints = readConstraints(widget);
        const current = readLayout(widget, breakpoint);
        let layout = { ...current };
        if (event.shiftKey) {
            if (key === 'ArrowRight') {
                layout.w += 1;
            } else if (key === 'ArrowLeft') {
                layout.w -= 1;
            } else if (key === 'ArrowDown') {
                layout.h += 1;
            } else if (key === 'ArrowUp') {
                layout.h -= 1;
            }
        } else {
            if (key === 'ArrowRight') {
                layout.x += 1;
            } else if (key === 'ArrowLeft') {
                layout.x -= 1;
            } else if (key === 'ArrowDown') {
                layout.y += 1;
            } else if (key === 'ArrowUp') {
                layout.y -= 1;
            }
        }

        const snapped = {
            x: clamp(layout.x, 1, Math.max(1, metrics.columns - layout.w + 1)),
            y: Math.max(1, layout.y),
            w: clamp(layout.w, constraints.minW, Math.min(constraints.maxW, metrics.columns)),
            h: clamp(layout.h, constraints.minH, constraints.maxH)
        };

        applyLayoutData(widget, breakpoint, snapped);
        notifyLayout(widget.dataset.widgetId, breakpoint, snapped);
        event.preventDefault();
    };

    state.gridElement.addEventListener('pointerdown', state.onPointerDown);
    window.addEventListener('pointermove', state.onPointerMove);
    window.addEventListener('pointerup', state.onPointerUp);
    state.gridElement.addEventListener('keydown', state.onKeyDown);

    gridElement.__reportLayoutState = state;
}

export function disposeGridLayout(gridElement) {
    if (!gridElement || !gridElement.__reportLayoutState) {
        return;
    }

    const state = gridElement.__reportLayoutState;
    state.gridElement.removeEventListener('pointerdown', state.onPointerDown);
    state.gridElement.removeEventListener('keydown', state.onKeyDown);
    window.removeEventListener('pointermove', state.onPointerMove);
    window.removeEventListener('pointerup', state.onPointerUp);

    delete gridElement.__reportLayoutState;
}
