import { useRef } from 'react'
import type { PinnedBundle } from './types'

/**
 * F0036-S0004 — pin-during-edit.
 *
 * The form binds to `(productVersionId, stage)` at OPEN and stays pinned for the
 * session (ADR-021). A newer product version activated elsewhere must not rebind
 * an open form: the pinned tuple is captured once (first render with a usable
 * pair) and never replaced, even if the inputs change. A new form instance (new
 * mount) pins whatever is active at its open. An unresolvable pinned version
 * surfaces a controlled error — never a silent fallback to a different version.
 */

/** The bundle payload for a resolved version (everything but the pinned tuple). */
export type ResolvedBundle = Omit<PinnedBundle, 'productVersionId' | 'stage'>

export interface UsePinnedBundleResult {
  /** The pinned bundle, or null while unresolved/errored. */
  bundle: PinnedBundle | null
  /** The tuple captured at open (stable for the session). */
  pinned: { productVersionId: string; stage: string } | null
  /** Controlled error when the pinned version cannot be resolved. */
  error: string | null
}

export function usePinnedBundle(
  productVersionId: string | null | undefined,
  stage: string | null | undefined,
  resolve: (productVersionId: string, stage: string) => ResolvedBundle | null,
): UsePinnedBundleResult {
  // Capture the tuple exactly once — the first render that has a usable pair.
  const pinnedRef = useRef<{ productVersionId: string; stage: string } | null>(null)
  if (pinnedRef.current === null && productVersionId && stage) {
    pinnedRef.current = { productVersionId, stage }
  }

  const pinned = pinnedRef.current
  if (!pinned) {
    return { bundle: null, pinned: null, error: null }
  }

  const resolved = resolve(pinned.productVersionId, pinned.stage)
  if (!resolved) {
    return {
      bundle: null,
      pinned,
      error: `Product definition unavailable for ${pinned.productVersionId} (${pinned.stage}).`,
    }
  }

  return {
    bundle: { ...resolved, productVersionId: pinned.productVersionId, stage: pinned.stage },
    pinned,
    error: null,
  }
}
