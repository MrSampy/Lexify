import { useTranslation } from 'react-i18next'
import { Mascot } from '@/shared/ui'
import { useImportWordsStore } from '../model/store'

export function FormatProgress() {
  const { t } = useTranslation()
  const streamingText = useImportWordsStore((s) => s.streamingText)

  // Show progress without exposing the raw JSON structure to the user: count how
  // many word objects have streamed in so far (each carries one "term" key).
  const wordsFound = (streamingText.match(/"term"\s*:/g) ?? []).length

  return (
    <div
      style={{
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        gap: 14,
        textAlign: 'center',
        padding: 'clamp(24px, 6vw, 48px) 16px',
      }}
    >
      <Mascot pose="diving" size={140} animate />

      <div className="ds-h4" style={{ margin: 0 }}>
        {t('import.formatting')}
      </div>
      <p className="ds-sm" style={{ margin: 0, color: 'var(--fg-3)', maxWidth: 360 }}>
        {t('import.formattingHint')}
      </p>

      {wordsFound > 0 && (
        <span
          aria-live="polite"
          style={{
            marginTop: 4,
            padding: '5px 14px',
            borderRadius: 'var(--r-pill)',
            background: 'var(--accent-ghost)',
            border: '1px solid var(--accent-line)',
            color: 'var(--accent-dim)',
            fontSize: 13,
            fontWeight: 700,
          }}
        >
          {t('import.wordsFound', { count: wordsFound })}
        </span>
      )}
    </div>
  )
}
