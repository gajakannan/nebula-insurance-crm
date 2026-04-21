declare module 'jest-axe' {
  export interface AxeResults {
    violations: unknown[]
  }

  export function axe(
    container: Element | DocumentFragment,
    options?: unknown,
  ): Promise<AxeResults>

  export const toHaveNoViolations: {
    toHaveNoViolations(results: AxeResults): {
      pass: boolean
      message: () => string
    }
  }
}
