import type { TargetAndTransition, Transition, Variants } from 'motion/react'

/**
 * Shared motion tokens and variants for the whole app. Every page/feature animation should build
 * from these so durations, easings, and spring feels stay consistent. Reduced-motion handling is
 * global: MotionProvider wraps the tree in <MotionConfig reducedMotion="user">.
 */

export const DUR = { fast: 0.15, base: 0.25, slow: 0.5 } as const

export const EASE = [0.22, 1, 0.36, 1] as const

export const SPRING: Transition = { type: 'spring', stiffness: 420, damping: 30 }

export const SPRING_BOUNCY: Transition = { type: 'spring', stiffness: 500, damping: 18 }

/** Single-element entrance: fade in while rising slightly. Pair with staggerContainer. */
export const fadeInUp: Variants = {
  hidden: { opacity: 0, y: 12 },
  visible: { opacity: 1, y: 0, transition: { duration: DUR.base, ease: EASE } },
}

/** Parent wrapper that staggers its fadeInUp (or popIn) children. */
export const staggerContainer = (stagger = 0.05): Variants => ({
  hidden: {},
  visible: { transition: { staggerChildren: stagger } },
})

/** Question card swap: slides in from the right, exits to the left. Key by question id. */
export const questionSlide: Variants = {
  enter: { opacity: 0, x: 40 },
  center: { opacity: 1, x: 0, transition: SPRING },
  exit: { opacity: 0, x: -40, transition: { duration: DUR.fast } },
}

/** Wrong-answer wobble — pass to `animate` directly, not as a variant. */
export const shake: TargetAndTransition = {
  x: [0, -8, 8, -6, 6, -3, 0],
  transition: { duration: 0.4 },
}

/** Scale pop for check marks, badges, and feedback icons. */
export const popIn: Variants = {
  hidden: { scale: 0, opacity: 0 },
  visible: { scale: 1, opacity: 1, transition: SPRING_BOUNCY },
}
