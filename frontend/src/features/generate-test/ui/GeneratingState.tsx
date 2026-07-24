import { useEffect, useState } from 'react'
import { AnimatePresence, motion } from 'motion/react'
import { useTranslation } from 'react-i18next'
import { Mascot } from '@/shared/ui'

const STEP_KEYS = ['testCreate.genStep1', 'testCreate.genStep2', 'testCreate.genStep3'] as const
const STEP_ROTATE_MS = 4000

/** Full-page waiting state while the backend job assembles the test (page keeps polling). */
export function GeneratingState() {
  const { t } = useTranslation()
  const [stepIndex, setStepIndex] = useState(0)

  useEffect(() => {
    const timer = setInterval(() => setStepIndex((i) => (i + 1) % STEP_KEYS.length), STEP_ROTATE_MS)
    return () => clearInterval(timer)
  }, [])

  return (
    <div className="flex min-h-[60vh] flex-col items-center justify-center gap-5">
      <Mascot pose="thinking" size={150} animate />

      <div className="ds-h3 flex items-baseline text-[var(--accent-color)]">
        {t('testCreate.generating')}
        {[0, 1, 2].map((i) => (
          <motion.span
            key={i}
            animate={{ opacity: [0.2, 1, 0.2] }}
            transition={{ duration: 1.2, repeat: Infinity, delay: i * 0.2 }}
          >
            .
          </motion.span>
        ))}
      </div>

      <div className="h-6">
        <AnimatePresence mode="wait">
          <motion.p
            key={stepIndex}
            initial={{ opacity: 0, y: 8 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -8 }}
            transition={{ duration: 0.3 }}
            className="ds-body m-0 text-[var(--fg-3)]"
          >
            {t(STEP_KEYS[stepIndex])}
          </motion.p>
        </AnimatePresence>
      </div>

      <div className="lx-progress-track w-64 max-w-full overflow-hidden">
        <motion.div
          className="lx-progress-fill h-full w-1/3"
          animate={{ x: ['-100%', '300%'] }}
          transition={{ duration: 1.4, repeat: Infinity, ease: 'easeInOut' }}
        />
      </div>
    </div>
  )
}
