import { MotionConfig } from 'motion/react'

/**
 * Globally disables transform/layout animations for users with prefers-reduced-motion. Imperative
 * effects that bypass the motion tree (e.g. canvas confetti) must guard themselves — see
 * shared/lib/confetti.ts.
 */
export function MotionProvider({ children }: { children: React.ReactNode }) {
  return <MotionConfig reducedMotion="user">{children}</MotionConfig>
}
