import confetti from 'canvas-confetti'

/**
 * Celebration burst for top test scores. No-ops for prefers-reduced-motion users — canvas-confetti
 * is imperative and invisible to MotionConfig's global reduced-motion handling, so the guard lives
 * here.
 */
export function fireConfetti() {
  if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) return

  const fire = (particleRatio: number, opts: confetti.Options) =>
    void confetti({
      origin: { y: 0.6 },
      particleCount: Math.floor(200 * particleRatio),
      ...opts,
    })

  fire(0.25, { spread: 26, startVelocity: 55 })
  fire(0.2, { spread: 60 })
  fire(0.35, { spread: 100, decay: 0.91, scalar: 0.8 })
  fire(0.1, { spread: 120, startVelocity: 25, decay: 0.92, scalar: 1.2 })
  fire(0.1, { spread: 120, startVelocity: 45 })
}
