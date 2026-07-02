interface AnswerFeedbackProps {
  isCorrect: boolean
  correctAnswer: string
  givenAnswer: string
  isLast: boolean
  onNext: () => void
}

export function AnswerFeedback({
  isCorrect,
  correctAnswer,
  givenAnswer,
  isLast,
  onNext,
}: AnswerFeedbackProps) {
  return (
    <div
      style={{
        padding: '16px 18px',
        background: isCorrect ? 'var(--success-ghost)' : 'var(--danger-ghost)',
        border: `1px solid ${isCorrect ? 'var(--accent-line)' : 'var(--danger)'}`,
        borderRadius: 'var(--r-md)',
        borderLeft: `3px solid ${isCorrect ? 'var(--success)' : 'var(--danger)'}`,
      }}
    >
      <div
        style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: isCorrect ? 12 : 8 }}
      >
        <span
          style={{
            fontSize: 16,
            color: isCorrect ? 'var(--success)' : 'var(--danger)',
          }}
        >
          {isCorrect ? '✓' : '✕'}
        </span>
        <span
          style={{
            fontFamily: 'var(--font-display)',
            fontWeight: 600,
            fontSize: 14,
            color: isCorrect ? 'var(--success)' : 'var(--danger)',
          }}
        >
          {isCorrect ? 'Correct!' : 'Incorrect'}
        </span>
      </div>
      {!isCorrect && (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 4, marginBottom: 14 }}>
          <div style={{ color: 'var(--fg-4)', fontSize: 11, fontWeight: 600 }}>
            your answer: <span style={{ color: 'var(--danger)' }}>{givenAnswer || '—'}</span>
          </div>
          <div style={{ color: 'var(--fg-4)', fontSize: 11, fontWeight: 600 }}>
            correct:{' '}
            <span style={{ color: 'var(--success)', fontWeight: 600 }}>{correctAnswer}</span>
          </div>
        </div>
      )}
      <button
        className="lx-btn-primary"
        style={{ padding: '8px 20px', fontSize: 13 }}
        onClick={onNext}
      >
        {isLast ? 'Finish →' : 'Next →'}
      </button>
    </div>
  )
}
