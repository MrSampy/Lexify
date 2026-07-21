import { motion } from 'motion/react'
import { Check } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { useBlocks } from '@/entities/block'
import { LANGUAGES } from '@/shared/config'
import { LanguageBadge, fadeInUp, popIn, staggerContainer } from '@/shared/ui'
import { cn } from '@/lib/utils'
import { useGenerateTestStore } from '../model/store'

export function BlockSelector() {
  const { t } = useTranslation()
  const { data, isLoading } = useBlocks({ page: 1, pageSize: 100 })
  const selectedBlockIds = useGenerateTestStore((s) => s.selectedBlockIds)
  const toggleBlock = useGenerateTestStore((s) => s.toggleBlock)

  if (isLoading) {
    return (
      <p className="text-xs font-semibold text-[var(--fg-4)]">{t('testCreate.loadingBlocks')}</p>
    )
  }

  if (!data || data.items.length === 0) {
    return <p className="text-xs font-semibold text-[var(--fg-4)]">{t('testCreate.noBlocks')}</p>
  }

  return (
    <motion.div
      variants={staggerContainer(0.04)}
      initial="hidden"
      animate="visible"
      className="grid grid-cols-1 gap-3 sm:grid-cols-2"
    >
      {data.items.map((block) => {
        const isSelected = selectedBlockIds.includes(block.id)
        const langCode = LANGUAGES[block.languageId]?.code
        return (
          <motion.button
            key={block.id}
            type="button"
            variants={fadeInUp}
            whileHover={{ y: -2 }}
            whileTap={{ scale: 0.98 }}
            role="checkbox"
            aria-checked={isSelected}
            onClick={() => toggleBlock(block.id)}
            className={cn(
              'relative cursor-pointer rounded-[var(--r-md)] border p-4 pr-9 text-left transition-colors',
              isSelected
                ? 'border-[var(--accent-color)] bg-[var(--accent-ghost)]'
                : 'border-[var(--line-2)] bg-[var(--bg-2)] hover:border-[var(--accent-line)]',
            )}
          >
            {isSelected && (
              <motion.span
                variants={popIn}
                initial="hidden"
                animate="visible"
                className="absolute top-2.5 right-2.5 flex size-5 items-center justify-center rounded-full bg-[var(--accent-color)] text-white"
              >
                <Check size={12} strokeWidth={3} />
              </motion.span>
            )}
            <p className="m-0 truncate text-sm font-semibold text-[var(--fg-1)]">{block.title}</p>
            <div className="mt-1.5 flex items-center gap-2">
              <span className="text-xs font-semibold text-[var(--fg-3)]">
                {t('testCreate.wordCount', { count: block.wordCount })}
              </span>
              {langCode && <LanguageBadge code={langCode} />}
            </div>
          </motion.button>
        )
      })}
    </motion.div>
  )
}
