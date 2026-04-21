import { useCallback, useLayoutEffect, useRef, useState } from 'react';
import type { RefCallback, RefObject } from 'react';

export interface MeasuredPoint {
  x: number;
  y: number;
}

export interface TimelineGeometry {
  /** Center X of the spine column, relative to container */
  spineX: number;
  /** Badge center positions keyed by stage status */
  stageCenters: Map<string, MeasuredPoint>;
  /** Spine cell center positions keyed by stage status */
  spineDots: Map<string, MeasuredPoint>;
  /** Outcome card center positions keyed by outcome key */
  outcomeCenters: Map<string, MeasuredPoint>;
  /** Top of the first spine cell */
  spineTop: number;
  /** Bottom of the last spine cell */
  spineBottom: number;
  /** Full container width */
  containerWidth: number;
  /** Full container height */
  containerHeight: number;
}

export interface UseTimelineGeometryResult {
  geometry: TimelineGeometry | null;
  registerBadge: (status: string) => RefCallback<HTMLElement>;
  registerSpineCell: (status: string) => RefCallback<HTMLElement>;
  registerOutcome: (key: string) => RefCallback<HTMLElement>;
}

function centerOf(el: HTMLElement, containerRect: DOMRect): MeasuredPoint {
  const rect = el.getBoundingClientRect();
  return {
    x: rect.left + rect.width / 2 - containerRect.left,
    y: rect.top + rect.height / 2 - containerRect.top,
  };
}

export function useTimelineGeometry(
  containerRef: RefObject<HTMLDivElement | null>,
): UseTimelineGeometryResult {
  const badgeElements = useRef(new Map<string, HTMLElement>());
  const spineCellElements = useRef(new Map<string, HTMLElement>());
  const outcomeElements = useRef(new Map<string, HTMLElement>());
  const [geometry, setGeometry] = useState<TimelineGeometry | null>(null);
  const rafId = useRef(0);

  const measureAll = useCallback(() => {
    const container = containerRef.current;
    if (!container) return;

    const containerRect = container.getBoundingClientRect();

    const stageCenters = new Map<string, MeasuredPoint>();
    badgeElements.current.forEach((el, status) => {
      stageCenters.set(status, centerOf(el, containerRect));
    });

    const spineDots = new Map<string, MeasuredPoint>();
    let spineTop = Infinity;
    let spineBottom = -Infinity;
    let spineX = containerRect.width / 2;

    spineCellElements.current.forEach((el, status) => {
      const point = centerOf(el, containerRect);
      spineDots.set(status, point);
      spineX = point.x; // all spine cells share the same X
      const elRect = el.getBoundingClientRect();
      const top = elRect.top - containerRect.top;
      const bottom = elRect.bottom - containerRect.top;
      if (top < spineTop) spineTop = top;
      if (bottom > spineBottom) spineBottom = bottom;
    });

    if (spineTop === Infinity) spineTop = 0;
    if (spineBottom === -Infinity) spineBottom = 0;

    const outcomeCenters = new Map<string, MeasuredPoint>();
    outcomeElements.current.forEach((el, key) => {
      outcomeCenters.set(key, centerOf(el, containerRect));
    });

    setGeometry({
      spineX,
      stageCenters,
      spineDots,
      outcomeCenters,
      spineTop,
      spineBottom,
      containerWidth: containerRect.width,
      containerHeight: containerRect.height,
    });
  }, [containerRef]);

  const scheduleMeasure = useCallback(() => {
    cancelAnimationFrame(rafId.current);
    rafId.current = requestAnimationFrame(measureAll);
  }, [measureAll]);

  // Measure after DOM mutation (before paint)
  useLayoutEffect(() => {
    measureAll();
  }, [measureAll]);

  // Re-measure on container resize
  useLayoutEffect(() => {
    const container = containerRef.current;
    if (!container || typeof ResizeObserver === 'undefined') return;

    const observer = new ResizeObserver(scheduleMeasure);
    observer.observe(container);
    return () => {
      cancelAnimationFrame(rafId.current);
      observer.disconnect();
    };
  }, [containerRef, scheduleMeasure]);

  const registerBadge = useCallback(
    (status: string): RefCallback<HTMLElement> =>
      (el) => {
        if (el) {
          badgeElements.current.set(status, el);
        } else {
          badgeElements.current.delete(status);
        }
      },
    [],
  );

  const registerSpineCell = useCallback(
    (status: string): RefCallback<HTMLElement> =>
      (el) => {
        if (el) {
          spineCellElements.current.set(status, el);
        } else {
          spineCellElements.current.delete(status);
        }
      },
    [],
  );

  const registerOutcome = useCallback(
    (key: string): RefCallback<HTMLElement> =>
      (el) => {
        if (el) {
          outcomeElements.current.set(key, el);
        } else {
          outcomeElements.current.delete(key);
        }
      },
    [],
  );

  return { geometry, registerBadge, registerSpineCell, registerOutcome };
}
