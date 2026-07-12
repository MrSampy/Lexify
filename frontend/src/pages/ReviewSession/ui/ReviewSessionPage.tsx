import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { ROUTES } from '@/shared/config'
import { SpeakButton, Spinner } from '@/shared/ui'
import { useDueWords, useRateWordMutation } from '@/features/review-word'
import type { Word } from '@/entities/word'

const RATER_LABELS: Record<number, string> = {
  0: 'review.rate0',
  1: 'review.rate1',
  2: 'review.rate2',
  3: 'review.rate3',
  4: 'review.rate4',
  5: 'review.rate5',
}

const RATER_COLORS = [
  'var(--danger)', // 0
  'var(--danger)', // 1
  'var(--warning)', // 2
  'var(--blue)', // 3
  'var(--success)', // 4
  'var(--accent-bright)', // 5
]

interface RatingEntry {
  wordId: string
  quality: number
}

// Matches .review-card-inner's transition duration in index.css — the card must
// finish flipping back to front before the next word's data is swapped in,
// otherwise the next word's translation briefly shows on the still-visible back face.
const FLIP_ANIMATION_MS = 420

export function ReviewSessionPage() {
  const { t } = useTranslation()
  const [currentIndex, setCurrentIndex] = useState(0)
  const [ratings, setRatings] = useState<RatingEntry[]>([])
  const [isFinished, setIsFinished] = useState(false)
  const [flipped, setFlipped] = useState(false)
  const [isAdvancing, setIsAdvancing] = useState(false)
  const advanceTimeoutRef = useRef<ReturnType<typeof setTimeout> | undefined>(undefined)

  const { data: dueWords, isLoading, isError } = useDueWords()
  const rateWord = useRateWordMutation()

  // Rating a word invalidates the due-words query, which would otherwise reshuffle
  // `words` mid-session (the just-rated word drops out of "due"). Freeze the list
  // once on load so `words[currentIndex]` always points at the intended word.
  // Adjusting state during render (rather than in an effect) avoids an extra
  // render pass — see https://react.dev/learn/you-might-not-need-an-effect.
  const [words, setWords] = useState<Word[] | null>(null)
  if (dueWords && words === null) {
    setWords(dueWords)
  }

  useEffect(() => () => clearTimeout(advanceTimeoutRef.current), [])

  const handleRate = (quality: number) => {
    const word = words![currentIndex]
    rateWord.mutate({ wordId: word.id, quality })
    const newRatings = [...ratings, { wordId: word.id, quality }]
    setRatings(newRatings)
    setFlipped(false)
    setIsAdvancing(true)

    advanceTimeoutRef.current = setTimeout(() => {
      setIsAdvancing(false)
      if (currentIndex + 1 === words!.length) {
        setIsFinished(true)
      } else {
        setCurrentIndex((i) => i + 1)
      }
    }, FLIP_ANIMATION_MS)
  }

  if (isFinished) {
    const hardCount = ratings.filter((r) => r.quality < 3).length
    const easyCount = ratings.filter((r) => r.quality >= 3).length
    return (
      <div className="flex min-h-[60vh] flex-col items-center justify-center gap-6 text-center">
        <div className="text-5xl">🎉</div>
        <div className="ds-h2">{t('review.complete')}</div>
        <p className="ds-body text-[var(--fg-3)]">
          {t('review.reviewed', { count: ratings.length })}
        </p>
        <div className="flex gap-8">
          <div className="text-center">
            <div className="text-[40px] font-bold text-[var(--danger)] [font-family:var(--font-display)]">
              {hardCount}
            </div>
            <div className="ds-sm font-semibold text-[var(--fg-3)]">{t('review.hard')}</div>
          </div>
          <div className="text-center">
            <div className="text-[40px] font-bold text-[var(--success)] [font-family:var(--font-display)]">
              {easyCount}
            </div>
            <div className="ds-sm font-semibold text-[var(--fg-3)]">{t('review.easy')}</div>
          </div>
        </div>
        <Link to={ROUTES.DASHBOARD} className="no-underline">
          <button className="lx-btn-primary">{t('review.backToDashboard')}</button>
        </Link>
      </div>
    )
  }

  if (isLoading) {
    return (
      <div style={{ display: 'flex', justifyContent: 'center', padding: '80px 0' }}>
        <Spinner size="lg" />
      </div>
    )
  }

  if (isError) {
    return (
      <div
        style={{
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          gap: 16,
          padding: '80px 0',
        }}
      >
        <p className="ds-sm" style={{ color: 'var(--fg-3)' }}>
          {t('review.loadFailed')}
        </p>
        <Link
          to={ROUTES.DASHBOARD}
          style={{ color: 'var(--accent-color)', textDecoration: 'none', fontWeight: 700 }}
        >
          {t('review.backToDashboard')}
        </Link>
      </div>
    )
  }

  if (words === null) {
    return (
      <div style={{ display: 'flex', justifyContent: 'center', padding: '80px 0' }}>
        <Spinner size="lg" />
      </div>
    )
  }

  if (words.length === 0) {
    return (
      <div className="flex min-h-[60vh] flex-col items-center justify-center gap-4 text-center">
        <div className="text-5xl">📖</div>
        <div className="ds-h3">{t('review.noneDue')}</div>
        <p className="ds-body text-[var(--fg-3)]">{t('review.comeBack')}</p>
        <Link to={ROUTES.BLOCKS} className="no-underline">
          <button className="lx-btn-secondary">{t('review.goToBlocks')}</button>
        </Link>
      </div>
    )
  }

  const currentWord = words[currentIndex]
  if (!currentWord) {
    return (
      <div style={{ display: 'flex', justifyContent: 'center', padding: '80px 0' }}>
        <Spinner size="lg" />
      </div>
    )
  }

  const progress = (currentIndex / words.length) * 100
  const remaining = words.length - currentIndex

  return (
    <div style={{ maxWidth: 900, margin: '0 auto' }}>
      {/* Header */}
      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          marginBottom: 8,
        }}
      >
        <div style={{ fontWeight: 700, color: 'var(--fg-2)', fontSize: 14 }}>
          {t('review.title')}
        </div>
        <span style={{ color: 'var(--accent-color)', fontSize: 13, fontWeight: 700 }}>
          {t('review.remaining', { count: remaining })}
        </span>
      </div>

      {/* Progress bar */}
      <div className="lx-progress-track" style={{ marginBottom: 30 }}>
        <div className="lx-progress-fill" style={{ width: `${progress}%` }} />
      </div>

      {/* Flashcard — clamp()-based sizing keeps it usable on phone screens */}
      <div className="review-card-wrap" style={{ marginBottom: 24, minHeight: 'min(480px, 65vh)' }}>
        <div
          className={`review-card-inner${flipped ? ' flipped' : ''}`}
          style={{ minHeight: 'min(480px, 65vh)' }}
        >
          {/* Front */}
          <div
            className="review-card-face"
            style={{
              background: 'var(--bg-2)',
              border: '1px solid var(--line-2)',
              borderRadius: 'var(--r-xl)',
              padding: 'clamp(28px, 7vw, 64px) clamp(16px, 5vw, 44px)',
              display: 'flex',
              flexDirection: 'column',
              alignItems: 'center',
              justifyContent: 'center',
              minHeight: 'min(480px, 65vh)',
            }}
          >
            <div
              style={{
                fontFamily: 'var(--font-display)',
                fontWeight: 600,
                fontSize: 'clamp(32px, 11vw, 60px)',
                color: 'var(--fg-1)',
                letterSpacing: '-0.02em',
                textAlign: 'center',
                overflowWrap: 'anywhere',
                maxWidth: '100%',
              }}
            >
              {currentWord.term}
            </div>
            <SpeakButton
              text={currentWord.term}
              languageId={currentWord.languageId}
              size={22}
              style={{ marginTop: 14 }}
            />
            <button
              className="lx-btn-secondary"
              style={{ marginTop: 30 }}
              onClick={() => setFlipped(true)}
            >
              {t('review.showTranslationFlip')}
            </button>
          </div>

          {/* Back */}
          <div
            className="review-card-back"
            style={{
              background: 'var(--bg-2)',
              border: '1px solid var(--accent-line)',
              borderRadius: 'var(--r-xl)',
              padding: 'clamp(24px, 6vw, 54px) clamp(16px, 5vw, 44px)',
              display: 'flex',
              flexDirection: 'column',
              alignItems: 'center',
              justifyContent: 'center',
              boxShadow: 'var(--glow-accent)',
              minHeight: 'min(480px, 65vh)',
            }}
          >
            <div style={{ color: 'var(--fg-4)', marginBottom: 8, fontSize: 13, fontWeight: 600 }}>
              {currentWord.term}
            </div>
            <div
              style={{
                fontFamily: 'var(--font-display)',
                fontWeight: 600,
                fontSize: 'clamp(26px, 9vw, 46px)',
                color: 'var(--accent-color)',
                letterSpacing: '-0.02em',
                textAlign: 'center',
                overflowWrap: 'anywhere',
                maxWidth: '100%',
              }}
            >
              {currentWord.translation}
            </div>
            {currentWord.notes && (
              <p
                className="ds-sm"
                style={{ color: 'var(--fg-3)', margin: '12px 0 0', textAlign: 'center' }}
              >
                {currentWord.notes}
              </p>
            )}
          </div>
        </div>
      </div>

      {/* Quality rater */}
      {flipped && (
        <div>
          <div
            style={{
              color: 'var(--fg-3)',
              textAlign: 'center',
              marginBottom: 12,
              fontSize: 13,
              fontWeight: 600,
            }}
          >
            {t('review.howWell')}
          </div>
          <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
            {[0, 1, 2, 3, 4, 5].map((n) => (
              <button
                key={n}
                onClick={() => handleRate(n)}
                disabled={rateWord.isPending || isAdvancing}
                style={{
                  flex: 1,
                  minWidth: 60,
                  display: 'flex',
                  flexDirection: 'column',
                  alignItems: 'center',
                  gap: 4,
                  padding: '14px 8px',
                  background: 'var(--bg-2)',
                  border: '1px solid var(--line-2)',
                  borderRadius: 'var(--r-md)',
                  cursor: 'pointer',
                  transition: 'transform 0.12s, border-color 0.12s',
                }}
                onMouseEnter={(e) => {
                  const el = e.currentTarget
                  el.style.transform = 'translateY(-2px)'
                  el.style.borderColor = RATER_COLORS[n]
                }}
                onMouseLeave={(e) => {
                  const el = e.currentTarget
                  el.style.transform = 'none'
                  el.style.borderColor = 'var(--line-2)'
                }}
              >
                <span
                  style={{
                    fontFamily: 'var(--font-display)',
                    fontWeight: 700,
                    fontSize: 22,
                    color: RATER_COLORS[n],
                  }}
                >
                  {n}
                </span>
                <span
                  style={{
                    fontFamily: 'var(--font-body)',
                    fontSize: 10,
                    color: 'var(--fg-3)',
                    fontWeight: 600,
                  }}
                >
                  {t(RATER_LABELS[n])}
                </span>
              </button>
            ))}
          </div>
        </div>
      )}
    </div>
  )
}
