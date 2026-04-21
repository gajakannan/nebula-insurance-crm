import React, { useEffect, useRef, useState, useCallback, useId } from 'react';
import { createPortal } from 'react-dom';
import { cn } from '@/lib/utils';

const FOCUSABLE_SELECTOR = [
  'a[href]',
  'button:not([disabled])',
  'textarea:not([disabled])',
  'input:not([disabled])',
  'select:not([disabled])',
  '[tabindex]:not([tabindex="-1"])',
].join(',');

interface PopoverProps {
  trigger: React.ReactNode;
  children: React.ReactNode;
  className?: string;
  contentAriaLabel?: string;
}

export function Popover({ trigger, children, className, contentAriaLabel }: PopoverProps) {
  const [open, setOpen] = useState(false);
  const triggerRef = useRef<HTMLElement | null>(null);
  const contentRef = useRef<HTMLDivElement | null>(null);
  const lastFocusedElementRef = useRef<HTMLElement | null>(null);
  const [rect, setRect] = useState<DOMRect | null>(null);
  const contentId = useId();
  const [viewportWidth, setViewportWidth] = useState(() =>
    typeof window === 'undefined' ? 1280 : window.innerWidth,
  );

  const close = useCallback(() => setOpen(false), []);

  const updatePosition = useCallback(() => {
    if (triggerRef.current) {
      setRect(triggerRef.current.getBoundingClientRect());
    }
  }, []);

  const handleToggle = useCallback(() => {
    setOpen((prev) => {
      if (!prev && triggerRef.current) {
        setRect(triggerRef.current.getBoundingClientRect());
      }
      return !prev;
    });
  }, []);

  useEffect(() => {
    function handleViewportResize() {
      setViewportWidth(window.innerWidth);
    }

    window.addEventListener('resize', handleViewportResize);
    return () => window.removeEventListener('resize', handleViewportResize);
  }, []);

  useEffect(() => {
    if (!open) return;

    updatePosition();
    lastFocusedElementRef.current = document.activeElement instanceof HTMLElement
      ? document.activeElement
      : null;

    requestAnimationFrame(() => {
      const container = contentRef.current;
      if (!container) {
        return;
      }

      const focusables = Array.from(container.querySelectorAll<HTMLElement>(FOCUSABLE_SELECTOR))
        .filter((element) => !element.hasAttribute('disabled') && element.tabIndex >= 0);
      const firstFocusable = focusables[0] ?? container;
      firstFocusable.focus();
    });

    function handleKeyDown(e: KeyboardEvent) {
      if (e.key === 'Escape') {
        e.preventDefault();
        close();
        return;
      }

      if (e.key !== 'Tab') {
        return;
      }

      const container = contentRef.current;
      if (!container) {
        return;
      }

      const focusables = Array.from(container.querySelectorAll<HTMLElement>(FOCUSABLE_SELECTOR))
        .filter((element) => !element.hasAttribute('disabled') && element.tabIndex >= 0);

      if (focusables.length === 0) {
        e.preventDefault();
        container.focus();
        return;
      }

      const first = focusables[0];
      const last = focusables[focusables.length - 1];
      const active = document.activeElement as HTMLElement | null;

      if (e.shiftKey && active === first) {
        e.preventDefault();
        last.focus();
      } else if (!e.shiftKey && active === last) {
        e.preventDefault();
        first.focus();
      }
    }

    function handleClickOutside(e: MouseEvent) {
      if (
        contentRef.current &&
        !contentRef.current.contains(e.target as Node) &&
        triggerRef.current &&
        !triggerRef.current.contains(e.target as Node)
      ) {
        close();
      }
    }

    document.addEventListener('keydown', handleKeyDown);
    document.addEventListener('mousedown', handleClickOutside);
    window.addEventListener('resize', updatePosition);
    window.addEventListener('scroll', updatePosition, true);
    return () => {
      document.removeEventListener('keydown', handleKeyDown);
      document.removeEventListener('mousedown', handleClickOutside);
      window.removeEventListener('resize', updatePosition);
      window.removeEventListener('scroll', updatePosition, true);
      requestAnimationFrame(() => {
        lastFocusedElementRef.current?.focus();
      });
    };
  }, [open, close, updatePosition]);

  const isNativeInteractiveElement = (node: React.ReactElement<Record<string, unknown>>) =>
    typeof node.type === 'string' && ['button', 'a', 'input', 'select', 'textarea'].includes(node.type);

  const assignRef = useCallback(
    (node: HTMLElement | null, externalRef?: React.Ref<HTMLElement>) => {
      triggerRef.current = node;
      if (!externalRef) {
        return;
      }

      if (typeof externalRef === 'function') {
        externalRef(node);
        return;
      }

      if (typeof externalRef === 'object') {
        (externalRef as { current: HTMLElement | null }).current = node;
      }
    },
    [],
  );

  // Attach ref + handlers directly to the trigger element (no wrapper div)
  const clonedTrigger = React.isValidElement(trigger)
    ? React.cloneElement(trigger as React.ReactElement<Record<string, unknown>>, {
        ref: (node: HTMLElement | null) =>
          assignRef(
            node,
            (trigger as React.ReactElement<Record<string, unknown>> & { ref?: React.Ref<HTMLElement> }).ref,
          ),
        'aria-haspopup': 'dialog',
        'aria-expanded': open,
        'aria-controls': open ? contentId : undefined,
        ...(isNativeInteractiveElement(trigger as React.ReactElement<Record<string, unknown>>)
          ? {}
          : { role: 'button', tabIndex: 0 }),
        onClick: (e: React.MouseEvent) => {
          const originalOnClick = (trigger as React.ReactElement<Record<string, unknown>>).props
            .onClick as ((e: React.MouseEvent) => void) | undefined;
          originalOnClick?.(e);
          handleToggle();
        },
        onKeyDown: (e: React.KeyboardEvent) => {
          const originalOnKeyDown = (trigger as React.ReactElement<Record<string, unknown>>).props
            .onKeyDown as ((e: React.KeyboardEvent) => void) | undefined;
          originalOnKeyDown?.(e);
          if (!isNativeInteractiveElement(trigger as React.ReactElement<Record<string, unknown>>) && (e.key === 'Enter' || e.key === ' ')) {
            e.preventDefault();
            handleToggle();
          }
        },
        className: cn(
          (trigger as React.ReactElement<Record<string, unknown>>).props.className as string | undefined,
          'cursor-pointer',
        ),
      })
    : (
        <button
          ref={triggerRef as React.RefObject<HTMLButtonElement>}
          type="button"
          aria-haspopup="dialog"
          aria-expanded={open}
          aria-controls={open ? contentId : undefined}
          onClick={handleToggle}
          className="cursor-pointer"
        >
          {trigger}
        </button>
      );

  const isPhone = viewportWidth < 640;
  const isTablet = viewportWidth >= 640 && viewportWidth < 1024;
  const renderAsOverlay = isPhone || isTablet;
  const minWidth = 280;
  const popoverLeft = rect
    ? Math.min(
      Math.max(8, rect.left),
      Math.max(8, viewportWidth - minWidth - 8),
    )
    : 8;

  return (
    <>
      {clonedTrigger}
      {open &&
        rect &&
        createPortal(
          <div className="fixed inset-0 z-50">
            {renderAsOverlay && (
              <button
                type="button"
                aria-label="Dismiss dialog"
                onClick={close}
                className="absolute inset-0 bg-black/35 backdrop-blur-[1px]"
              />
            )}
            <div
              id={contentId}
              ref={contentRef}
              role="dialog"
              aria-modal={renderAsOverlay}
              aria-label={contentAriaLabel}
              tabIndex={-1}
              className={cn(
                'fixed z-[51] glass-card popover-glass rounded-xl p-4 shadow-2xl',
                isTablet && 'left-1/2 top-1/2 w-[min(28rem,calc(100vw-2rem))] -translate-x-1/2 -translate-y-1/2',
                isPhone && 'inset-x-0 bottom-0 max-h-[82dvh] overflow-y-auto rounded-t-2xl rounded-b-none',
                className,
              )}
              style={renderAsOverlay
                ? undefined
                : {
                  top: rect.bottom + 8,
                  left: popoverLeft,
                  minWidth,
                }}
            >
              {children}
            </div>
          </div>,
          document.body,
        )}
    </>
  );
}
