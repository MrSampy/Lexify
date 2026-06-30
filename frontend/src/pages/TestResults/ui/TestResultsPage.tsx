import { Link, useNavigate, useParams } from 'react-router-dom'
import { ROUTES } from '@/shared/config'
import { Spinner } from '@/shared/ui'
import { useAttemptResults } from '@/entities/test'

export function TestResultsPage() {
  const { id: attemptId } = useParams<{ id: string }>()
  const navigate = useNavigate()

  const { data, isLoading, isError } = useAttemptResults(attemptId ?? '')

  if (isLoading) {
    return (
      <div style={{ display: 'flex', justifyContent: 'center', padding: '80px 0' }}>
        <Spinner size="lg" />
      </div>
    )
  }

  if (isError || !data) {
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
          Results not available yet.
        </p>
        <Link
          to={ROUTES.TESTS}
          className="ds-code"
          style={{ color: 'var(--accent-color)', textDecoration: 'none' }}
        >
          ← Back to tests
        </Link>
      </div>
    )
  }

  const scorePercent = Math.round(data.score * 100)
  const wrongAnswers = data.answers.filter((a) => !a.isCorrect)

  const getMessage = () => {
    if (scorePercent >= 90) return 'Excellent session!'
    if (scorePercent >= 60) return 'Good work!'
    return 'Keep practicing!'
  }

  return (
    <div style={{ maxWidth: 720, margin: '0 auto' }}>
      <div
        className="eyebrow"
        style={{ marginBottom: 14, justifyContent: 'center', textAlign: 'center' }}
      >
        ~/tests/results
      </div>

      {/* Score circle */}
      <div
        style={{
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          textAlign: 'center',
          marginBottom: 30,
        }}
      >
        <div style={{ position: 'relative', width: 148, height: 148, marginBottom: 18 }}>
          <div
            style={{
              position: 'absolute',
              inset: 0,
              borderRadius: '50%',
              background: `conic-gradient(var(--accent-color) 0% ${scorePercent}%, var(--bg-3) ${scorePercent}% 100%)`,
            }}
          />
          <div
            style={{
              position: 'absolute',
              inset: 10,
              borderRadius: '50%',
              background: 'var(--bg-1)',
              display: 'flex',
              flexDirection: 'column',
              alignItems: 'center',
              justifyContent: 'center',
            }}
          >
            <span
              style={{
                fontFamily: 'var(--font-display)',
                fontWeight: 700,
                fontSize: 40,
                color: 'var(--fg-1)',
              }}
            >
              {scorePercent}%
            </span>
            <span className="ds-code" style={{ color: 'var(--fg-3)' }}>
              {data.correctAnswers} / {data.totalQuestions}
            </span>
          </div>
        </div>

        <h1 className="ds-h3" style={{ margin: '0 0 4px' }}>
          {getMessage()}
        </h1>
        <p className="ds-body" style={{ margin: 0, color: 'var(--fg-3)' }}>
          {wrongAnswers.length > 0
            ? `${wrongAnswers.length} words moved to shorter review interval.`
            : 'All answers correct — great job!'}
        </p>
      </div>

      {/* Answer breakdown */}
      {data.answers.length > 0 && (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 8, marginBottom: 24 }}>
          {data.answers.map((r) => (
            <div
              key={r.questionId}
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: 14,
                padding: '13px 18px',
                background: 'var(--bg-2)',
                border: '1px solid var(--line-2)',
                borderRadius: 'var(--r-md)',
                borderLeft: `3px solid ${r.isCorrect ? 'var(--success)' : 'var(--danger)'}`,
              }}
            >
              <span
                style={{
                  fontFamily: 'var(--font-mono)',
                  color: r.isCorrect ? 'var(--success)' : 'var(--danger)',
                  fontSize: 15,
                  width: 16,
                }}
              >
                {r.isCorrect ? '✓' : '✕'}
              </span>
              <span style={{ flex: 1, color: 'var(--fg-1)', fontSize: 14 }}>{r.questionText}</span>
              {!r.isCorrect && (
                <div style={{ textAlign: 'right' }}>
                  <div className="ds-code" style={{ color: 'var(--danger)', fontSize: 11 }}>
                    {r.givenAnswer || '—'}
                  </div>
                  <div className="ds-code" style={{ color: 'var(--success)', fontSize: 11 }}>
                    {r.correctAnswer}
                  </div>
                </div>
              )}
            </div>
          ))}
        </div>
      )}

      {/* Actions */}
      <div style={{ display: 'flex', gap: 12, justifyContent: 'center' }}>
        <button
          className="lx-btn-secondary"
          onClick={() => navigate(ROUTES.TEST_RUNNER(data.testId))}
        >
          ↺ Try again
        </button>
        <button className="lx-btn-primary" onClick={() => navigate(ROUTES.TESTS)}>
          Back to tests
        </button>
      </div>
    </div>
  )
}
