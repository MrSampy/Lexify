import { useEffect, useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import { ROUTES } from '@/shared/config'
import { Spinner } from '@/shared/ui'
import { useDueWords, useRateWordMutation } from '@/features/review-word'
import type { Word } from '@/entities/word'

const RATER_LABELS: Record<number, string> = {
  0: 'Blackout',
  1: 'Barely',
  2: 'Effort',
  3: 'Hesitant',
  4: 'Easy',
  5: 'Perfect',
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
        <div className="ds-h2">Session complete!</div>
        <p className="ds-body text-[var(--fg-3)]">Reviewed {ratings.length} words</p>
        <div className="flex gap-8">
          <div className="text-center">
            <div className="text-[40px] font-bold text-[var(--danger)] [font-family:var(--font-display)]">
              {hardCount}
            </div>
            <div className="ds-sm font-semibold text-[var(--fg-3)]">hard (0–2)</div>
          </div>
          <div className="text-center">
            <div className="text-[40px] font-bold text-[var(--success)] [font-family:var(--font-display)]">
              {easyCount}
            </div>
            <div className="ds-sm font-semibold text-[var(--fg-3)]">easy (3–5)</div>
          </div>
        </div>
        <Link to={ROUTES.DASHBOARD} className="no-underline">
          <button className="lx-btn-primary">Back to dashboard</button>
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
          Failed to load words for review.
        </p>
        <Link
          to={ROUTES.DASHBOARD}
          style={{ color: 'var(--accent-color)', textDecoration: 'none', fontWeight: 700 }}
        >
          Back to dashboard
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
        <div className="ds-h3">No words due for review!</div>
        <p className="ds-body text-[var(--fg-3)]">Come back later or add new words.</p>
        <Link to={ROUTES.BLOCKS} className="no-underline">
          <button className="lx-btn-secondary">Go to blocks</button>
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
    <div style={{ maxWidth: 560, margin: '0 auto' }}>
      {/* Header */}
      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          marginBottom: 8,
        }}
      >
        <div style={{ fontWeight: 700, color: 'var(--fg-2)', fontSize: 14 }}>Review 🔄</div>
        <span style={{ color: 'var(--accent-color)', fontSize: 13, fontWeight: 700 }}>
          {remaining} words remaining
        </span>
      </div>

      {/* Progress bar */}
      <div className="lx-progress-track" style={{ marginBottom: 30 }}>
        <div className="lx-progress-fill" style={{ width: `${progress}%` }} />
      </div>

      {/* Flashcard */}
      <div className="review-card-wrap" style={{ marginBottom: 24, minHeight: 280 }}>
        <div className={`review-card-inner${flipped ? ' flipped' : ''}`} style={{ minHeight: 280 }}>
          {/* Front */}
          <div
            className="review-card-face"
            style={{
              background: 'var(--bg-2)',
              border: '1px solid var(--line-2)',
              borderRadius: 'var(--r-xl)',
              padding: '46px 30px',
              display: 'flex',
              flexDirection: 'column',
              alignItems: 'center',
              justifyContent: 'center',
              minHeight: 280,
            }}
          >
            <div
              style={{
                fontFamily: 'var(--font-display)',
                fontWeight: 600,
                fontSize: 42,
                color: 'var(--fg-1)',
                letterSpacing: '-0.02em',
                textAlign: 'center',
              }}
            >
              {currentWord.term}
            </div>
            <button
              className="lx-btn-secondary"
              style={{ marginTop: 30 }}
              onClick={() => setFlipped(true)}
            >
              Show translation ↻
            </button>
          </div>

          {/* Back */}
          <div
            className="review-card-back"
            style={{
              background: 'var(--bg-2)',
              border: '1px solid var(--accent-line)',
              borderRadius: 'var(--r-xl)',
              padding: '36px 30px',
              display: 'flex',
              flexDirection: 'column',
              alignItems: 'center',
              justifyContent: 'center',
              boxShadow: 'var(--glow-accent)',
              minHeight: 280,
            }}
          >
            <div style={{ color: 'var(--fg-4)', marginBottom: 8, fontSize: 13, fontWeight: 600 }}>
              {currentWord.term}
            </div>
            <div
              style={{
                fontFamily: 'var(--font-display)',
                fontWeight: 600,
                fontSize: 32,
                color: 'var(--accent-color)',
                letterSpacing: '-0.02em',
                textAlign: 'center',
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
            How well did you recall it?
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
                  {RATER_LABELS[n]}
                </span>
              </button>
            ))}
          </div>
        </div>
      )}
    </div>
  )
}
