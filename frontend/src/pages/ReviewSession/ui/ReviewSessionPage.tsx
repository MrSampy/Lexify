import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useSearchParams } from 'react-router-dom'
import { ROUTES } from '@/shared/config'
import { useSpeak } from '@/shared/lib'
import { SpeakButton, Spinner } from '@/shared/ui'
import { useDueWords, useRateWordMutation } from '@/features/review-word'
import type { Word } from '@/entities/word'

const AUTO_SPEAK_KEY = 'lexify_review_autospeak'

// Anki-style 4-button scale mapped onto SM-2 quality. 0/3/4/5 preserves every
// downstream semantic: q < 3 is a lapse in SpacedRepetitionService and stats
// count q >= 3 as a correct recall.
const RATERS = [
  { quality: 0, labelKey: 'review.rateAgain', color: 'var(--danger)', hotkey: '1' },
  { quality: 3, labelKey: 'review.rateHard', color: 'var(--warning)', hotkey: '2' },
  { quality: 4, labelKey: 'review.rateGood', color: 'var(--success)', hotkey: '3' },
  { quality: 5, labelKey: 'review.rateEasy', color: 'var(--accent-bright)', hotkey: '4' },
] as const

interface RatingEntry {
  wordId: string
  term: string
  quality: number
  /** Days until the next review, filled in when the rate request resolves. */
  intervalDays?: number
}

// Matches .review-card-inner's transition duration in index.css — the card must
// finish flipping back to front before the next word's data is swapped in,
// otherwise the next word's translation briefly shows on the still-visible back face.
const FLIP_ANIMATION_MS = 420

export function ReviewSessionPage() {
  const { t } = useTranslation()
  const [searchParams] = useSearchParams()
  const blockId = searchParams.get('blockId') ?? undefined
  const cram = searchParams.get('mode') === 'cram'

  const [currentIndex, setCurrentIndex] = useState(0)
  const [ratings, setRatings] = useState<RatingEntry[]>([])
  const [isFinished, setIsFinished] = useState(false)
  const [flipped, setFlipped] = useState(false)
  const [isAdvancing, setIsAdvancing] = useState(false)
  const [autoSpeak, setAutoSpeak] = useState(() => localStorage.getItem(AUTO_SPEAK_KEY) === '1')
  const [lastRated, setLastRated] = useState<{ term: string; intervalDays: number } | null>(null)
  const advanceTimeoutRef = useRef<ReturnType<typeof setTimeout> | undefined>(undefined)

  const { data: queue, isLoading, isError } = useDueWords({ blockId, cram })
  const rateWord = useRateWordMutation()

  // Rating a word invalidates the due-words query, which would otherwise reshuffle
  // `words` mid-session (the just-rated word drops out of "due"). Freeze the list
  // once on load so `words[currentIndex]` always points at the intended word.
  // Adjusting state during render (rather than in an effect) avoids an extra
  // render pass — see https://react.dev/learn/you-might-not-need-an-effect.
  const [words, setWords] = useState<Word[] | null>(null)
  const [composition, setComposition] = useState<{ newCount: number; reviewCount: number } | null>(
    null,
  )
  if (queue && words === null) {
    setWords(queue.words)
    setComposition({ newCount: queue.newCount, reviewCount: queue.reviewCount })
  }

  const currentWord = words && !isFinished ? words[currentIndex] : undefined

  // Term-language TTS for the visible card — server neural audio when available, browser fallback.
  const { speak } = useSpeak({ wordId: currentWord?.id, languageId: currentWord?.languageId })

  useEffect(() => () => clearTimeout(advanceTimeoutRef.current), [])

  const toggleAutoSpeak = () => {
    setAutoSpeak((prev) => {
      const next = !prev
      localStorage.setItem(AUTO_SPEAK_KEY, next ? '1' : '0')
      return next
    })
  }

  const handleRate = (quality: number) => {
    const word = words![currentIndex]
    rateWord.mutate(
      { wordId: word.id, quality },
      {
        onSuccess: (data) => {
          setRatings((prev) =>
            prev.map((r) => (r.wordId === word.id ? { ...r, intervalDays: data.intervalDays } : r)),
          )
          setLastRated({ term: word.term, intervalDays: data.intervalDays })
        },
      },
    )
    const newRatings = [...ratings, { wordId: word.id, term: word.term, quality }]
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

  // Restart the session with only the words rated hard (quality < 3) this round.
  const replayHard = () => {
    const hardIds = new Set(ratings.filter((r) => r.quality < 3).map((r) => r.wordId))
    const hardWords = (words ?? []).filter((w) => hardIds.has(w.id))
    if (hardWords.length === 0) return
    setWords(hardWords)
    setComposition(null)
    setCurrentIndex(0)
    setRatings([])
    setFlipped(false)
    setIsFinished(false)
    setLastRated(null)
  }

  // Auto-play the term whenever a fresh (front-facing) card appears, if the user enabled it.
  useEffect(() => {
    if (autoSpeak && currentWord && !flipped) void speak(currentWord.term)
    // Trigger on card change / toggle only — not on every flip back.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [currentWord?.id, autoSpeak])

  // Keyboard shortcuts: Space/Enter flips the card; digit keys 0–5 rate it once flipped.
  useEffect(() => {
    function onKey(e: KeyboardEvent) {
      const el = e.target as HTMLElement | null
      if (el && (el.tagName === 'INPUT' || el.tagName === 'TEXTAREA' || el.isContentEditable))
        return
      if (!currentWord || isAdvancing) return

      if (!flipped) {
        if (e.code === 'Space' || e.key === 'Enter') {
          e.preventDefault()
          setFlipped(true)
        }
        return
      }
      const rater = RATERS.find((r) => r.hotkey === e.key)
      if (rater) {
        e.preventDefault()
        handleRate(rater.quality)
      }
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [flipped, currentWord?.id, isAdvancing])

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
        {ratings.some((r) => r.intervalDays !== undefined) && (
          <div
            style={{
              maxHeight: 220,
              overflowY: 'auto',
              width: 'min(420px, 90vw)',
              border: '1px solid var(--line-2)',
              borderRadius: 'var(--r-md)',
              padding: '10px 14px',
              textAlign: 'left',
            }}
          >
            {ratings.map((r) => (
              <div
                key={r.wordId}
                style={{
                  display: 'flex',
                  justifyContent: 'space-between',
                  gap: 12,
                  fontSize: 13,
                  padding: '3px 0',
                }}
              >
                <span
                  style={{
                    color: r.quality < 3 ? 'var(--danger)' : 'var(--fg-2)',
                    fontWeight: 600,
                    overflow: 'hidden',
                    textOverflow: 'ellipsis',
                    whiteSpace: 'nowrap',
                  }}
                >
                  {r.term}
                </span>
                {r.intervalDays !== undefined && (
                  <span style={{ color: 'var(--fg-4)', flexShrink: 0 }}>
                    {t('review.nextIn', { count: r.intervalDays })}
                  </span>
                )}
              </div>
            ))}
          </div>
        )}
        <div className="flex flex-wrap items-center justify-center gap-3">
          {hardCount > 0 && (
            <button className="lx-btn-secondary" onClick={replayHard}>
              {t('review.replayHard')}
            </button>
          )}
          <Link to={ROUTES.DASHBOARD} className="no-underline">
            <button className="lx-btn-primary">{t('review.backToDashboard')}</button>
          </Link>
        </div>
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
        <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
          <div style={{ fontWeight: 700, color: 'var(--fg-2)', fontSize: 14 }}>
            {t('review.title')}
          </div>
          {cram && (
            <span
              style={{
                fontSize: 11,
                fontWeight: 800,
                padding: '2px 10px',
                borderRadius: 'var(--r-pill)',
                background: 'var(--warning-ghost)',
                color: 'var(--warning)',
              }}
            >
              {t('review.cramBadge')}
            </span>
          )}
          {!cram && composition && (
            <span
              style={{
                fontSize: 11,
                fontWeight: 700,
                padding: '2px 10px',
                borderRadius: 'var(--r-pill)',
                background: 'var(--accent-ghost)',
                color: 'var(--accent-dim)',
              }}
            >
              {t('review.queueComposition', {
                newCount: composition.newCount,
                reviewCount: composition.reviewCount,
              })}
            </span>
          )}
        </div>
        <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
          <button
            onClick={toggleAutoSpeak}
            aria-pressed={autoSpeak}
            style={{
              border: '1.5px solid var(--line-2)',
              background: autoSpeak ? 'var(--accent-ghost)' : 'var(--bg-1)',
              color: autoSpeak ? 'var(--accent-dim)' : 'var(--fg-3)',
              fontSize: 12,
              fontWeight: 700,
              padding: '4px 10px',
              borderRadius: 'var(--r-pill)',
              cursor: 'pointer',
            }}
          >
            🔊 {t('review.autoSpeak')}
          </button>
          <span style={{ color: 'var(--accent-color)', fontSize: 13, fontWeight: 700 }}>
            {t('review.remaining', { count: remaining })}
          </span>
        </div>
      </div>

      {/* Progress bar */}
      <div className="lx-progress-track" style={{ marginBottom: lastRated ? 8 : 30 }}>
        <div className="lx-progress-fill" style={{ width: `${progress}%` }} />
      </div>

      {/* Last-rated feedback: the new SM-2 interval for the word just answered */}
      {lastRated && (
        <div
          style={{
            textAlign: 'center',
            marginBottom: 16,
            fontSize: 12,
            fontWeight: 600,
            color: 'var(--fg-4)',
          }}
        >
          {lastRated.term} · {t('review.nextIn', { count: lastRated.intervalDays })}
        </div>
      )}

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
              wordId={currentWord.id}
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
            {currentWord.exampleSentence && (
              <p
                className="ds-sm"
                style={{
                  color: 'var(--fg-4)',
                  margin: '14px 0 0',
                  textAlign: 'center',
                  fontStyle: 'italic',
                  maxWidth: 520,
                }}
              >
                “{currentWord.exampleSentence}”
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
              marginBottom: 4,
              fontSize: 13,
              fontWeight: 600,
            }}
          >
            {t('review.howWell')}
          </div>
          <div
            style={{
              color: 'var(--fg-4)',
              textAlign: 'center',
              marginBottom: 12,
              fontSize: 11,
            }}
          >
            {t('review.keyboardHint')}
          </div>
          <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
            {RATERS.map((rater) => (
              <button
                key={rater.quality}
                onClick={() => handleRate(rater.quality)}
                disabled={rateWord.isPending || isAdvancing}
                style={{
                  flex: 1,
                  minWidth: 90,
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
                  el.style.borderColor = rater.color
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
                    fontSize: 18,
                    color: rater.color,
                  }}
                >
                  {t(rater.labelKey)}
                </span>
                <span
                  style={{
                    fontFamily: 'var(--font-body)',
                    fontSize: 10,
                    color: 'var(--fg-4)',
                    fontWeight: 600,
                  }}
                >
                  {rater.hotkey}
                </span>
              </button>
            ))}
          </div>
        </div>
      )}
    </div>
  )
}
