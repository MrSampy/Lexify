import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Button } from '@/shared/ui'
import type { Word } from '@/entities/word'

interface ReviewCardProps {
  word: Word
}

export function ReviewCard({ word }: ReviewCardProps) {
  const { t } = useTranslation()
  const [isFlipped, setIsFlipped] = useState(false)

  return (
    <div className="rounded-xl border bg-card p-8 shadow-sm">
      {!isFlipped ? (
        <div className="flex min-h-40 flex-col items-center justify-center gap-6 text-center">
          <p className="ds-h2">{word.term}</p>
          <Button variant="outline" onClick={() => setIsFlipped(true)}>
            {t('review.showTranslation')}
          </Button>
        </div>
      ) : (
        <div className="flex min-h-40 flex-col gap-4 text-center">
          <p className="ds-h2" style={{ color: 'var(--accent-color)' }}>
            {word.translation}
          </p>
          {word.notes && <p className="text-sm text-muted-foreground">{word.notes}</p>}
          {word.exampleSentence && (
            <p className="text-sm italic text-muted-foreground">"{word.exampleSentence}"</p>
          )}
          <div className="mt-2">
            <Button variant="ghost" size="sm" onClick={() => setIsFlipped(false)}>
              {t('review.hide')}
            </Button>
          </div>
        </div>
      )}
    </div>
  )
}
