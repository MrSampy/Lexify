import { useTranslation } from 'react-i18next'
import { Dialog, DialogContent, DialogHeader, DialogTitle, Spinner } from '@/shared/ui'
import { formatDate } from '@/shared/lib'
import { useWordHistory } from '../api/wordApi'
import type { Word } from '../model/types'

// Same colors as the review-session rating buttons: lapse → danger, hesitant → warning, good+ → success.
function qualityColor(quality: number) {
  if (quality < 3) return 'var(--danger)'
  if (quality === 3) return 'var(--warning)'
  return 'var(--success)'
}

interface WordHistoryPanelProps {
  word: Word
  open: boolean
  onOpenChange: (open: boolean) => void
}

/** Dialog showing a word's SM-2 review trajectory from the immutable review log. */
export function WordHistoryPanel({ word, open, onOpenChange }: WordHistoryPanelProps) {
  const { t } = useTranslation()
  const { data: history, isLoading, isError } = useWordHistory(word.id, open)

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent style={{ maxWidth: 520 }}>
        <DialogHeader>
          <DialogTitle>
            {word.term}
            <span style={{ color: 'var(--fg-4)', fontWeight: 400 }}> — {word.translation}</span>
          </DialogTitle>
        </DialogHeader>

        <div
          className="ds-sm"
          style={{ display: 'flex', gap: 16, flexWrap: 'wrap', color: 'var(--fg-3)' }}
        >
          <span>
            {t('words.historyInterval')}: <b>{word.intervalDays}d</b>
          </span>
          <span>
            {t('words.historyEase')}: <b>{word.easeFactor.toFixed(2)}</b>
          </span>
          <span>
            {t('words.historyReps')}: <b>{word.repetitions}</b>
          </span>
        </div>

        {isLoading ? (
          <div style={{ display: 'flex', justifyContent: 'center', padding: 24 }}>
            <Spinner />
          </div>
        ) : isError ? (
          <p className="ds-sm" style={{ color: 'var(--danger)' }}>
            {t('words.historyFailed')}
          </p>
        ) : !history || history.length === 0 ? (
          <p className="ds-sm" style={{ color: 'var(--fg-3)' }}>
            {t('words.historyEmpty')}
          </p>
        ) : (
          <div style={{ maxHeight: 320, overflowY: 'auto' }}>
            {history.map((entry, i) => (
              <div
                key={`${entry.reviewedAt}-${i}`}
                style={{
                  display: 'flex',
                  alignItems: 'center',
                  gap: 10,
                  padding: '7px 2px',
                  borderTop: '1px solid var(--line-1)',
                  fontSize: 13,
                }}
              >
                <span style={{ color: 'var(--fg-3)', width: 92, flexShrink: 0 }}>
                  {formatDate(entry.reviewedAt)}
                </span>
                <span
                  title={`quality ${entry.quality}`}
                  style={{
                    width: 22,
                    height: 22,
                    borderRadius: 6,
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    flexShrink: 0,
                    fontWeight: 800,
                    fontSize: 12,
                    color: qualityColor(entry.quality),
                    background: 'var(--bg-3)',
                  }}
                >
                  {entry.quality}
                </span>
                <span
                  style={{
                    fontSize: 11,
                    fontWeight: 700,
                    padding: '1px 8px',
                    borderRadius: 'var(--r-pill)',
                    background: 'var(--bg-3)',
                    color: 'var(--fg-3)',
                    flexShrink: 0,
                  }}
                >
                  {t(`words.historySource.${entry.source}`, { defaultValue: entry.source })}
                </span>
                <span style={{ flex: 1 }} />
                <span style={{ color: 'var(--fg-2)', fontWeight: 600, flexShrink: 0 }}>
                  → {entry.intervalDaysAfter}d
                </span>
                <span
                  style={{ color: 'var(--fg-4)', flexShrink: 0, width: 48, textAlign: 'right' }}
                >
                  {entry.easeFactorAfter.toFixed(2)}
                </span>
              </div>
            ))}
          </div>
        )}
      </DialogContent>
    </Dialog>
  )
}
