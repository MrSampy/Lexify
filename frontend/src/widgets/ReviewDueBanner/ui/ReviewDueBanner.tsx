import { useNavigate } from 'react-router-dom'
import { ROUTES } from '@/shared/config'
import { useDueWords } from '@/features/review-word'

export function ReviewDueBanner() {
  const navigate = useNavigate()
  const { data, isLoading } = useDueWords()

  if (isLoading || !data || data.length === 0) return null

  return (
    <div
      style={{
        display: 'flex',
        alignItems: 'center',
        gap: 16,
        padding: '16px 20px',
        background: 'var(--accent-ghost)',
        border: '1px solid var(--accent-line)',
        borderRadius: 'var(--r-lg)',
      }}
    >
      <div
        style={{
          width: 38,
          height: 38,
          flexShrink: 0,
          borderRadius: 'var(--r-md)',
          background: 'var(--bg-1)',
          border: '1px solid var(--accent-line)',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          color: 'var(--accent-color)',
          fontFamily: 'var(--font-mono)',
          fontSize: 16,
        }}
      >
        ▶
      </div>
      <div style={{ flex: 1 }}>
        <div className="ds-h4" style={{ color: 'var(--fg-1)' }}>
          {data.length} {data.length === 1 ? 'word' : 'words'} due for review today
        </div>
        <div className="ds-code" style={{ color: 'var(--fg-3)', marginTop: 2 }}>
          SM-2 — only what's actually due
        </div>
      </div>
      <button
        className="lx-btn-primary"
        style={{ padding: '9px 18px', flexShrink: 0 }}
        onClick={() => navigate(ROUTES.REVIEW)}
      >
        Start review →
      </button>
    </div>
  )
}
