import { useTranslation } from 'react-i18next'
import type { ConversationDetail } from '@/entities/conversation'

/** Read-only view of an ended conversation: header with date/score + the full transcript. */
export function ReadOnlyTranscript({ detail }: { detail: ConversationDetail }) {
  const { t, i18n } = useTranslation()
  const endedDate = detail.endedAt
    ? new Date(detail.endedAt).toLocaleDateString(i18n.language, {
        day: 'numeric',
        month: 'long',
        year: 'numeric',
      })
    : null

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
      {/* Session header */}
      <div
        className="lx-card"
        style={{
          display: 'flex',
          flexWrap: 'wrap',
          alignItems: 'center',
          justifyContent: 'space-between',
          gap: 12,
          padding: '14px 18px',
        }}
      >
        <div style={{ minWidth: 0 }}>
          <div style={{ fontWeight: 700, fontSize: 15, color: 'var(--fg-1)' }}>{detail.title}</div>
          <div className="ds-sm" style={{ color: 'var(--fg-4)', marginTop: 2 }}>
            {endedDate ?? t('chat.statusEnded')} ·{' '}
            {t('chat.messagesCount', { count: detail.messages.length })}
          </div>
        </div>
        <div style={{ display: 'flex', alignItems: 'center', gap: 14, flexShrink: 0 }}>
          {detail.stars != null && (
            <span aria-label={`${detail.stars}/3`} style={{ fontSize: 18, letterSpacing: 2 }}>
              {[1, 2, 3].map((n) => (
                <span
                  key={n}
                  style={{ color: n <= (detail.stars ?? 0) ? 'var(--warning)' : 'var(--line-2)' }}
                >
                  ★
                </span>
              ))}
            </span>
          )}
          {detail.points != null && (
            <span
              style={{
                fontFamily: 'var(--font-display)',
                fontWeight: 800,
                fontSize: 16,
                color: 'var(--accent-color)',
              }}
            >
              {t('chat.pointsValue', { points: detail.points })}
            </span>
          )}
        </div>
      </div>

      {/* Target words practised in this session */}
      {detail.targetWords.length > 0 && (
        <div style={{ display: 'flex', flexWrap: 'wrap', gap: 6, alignItems: 'center' }}>
          <span style={{ fontSize: 12, fontWeight: 700, color: 'var(--fg-4)' }}>
            {t('chat.wordsToUse')}
          </span>
          {detail.targetWords.map((w) => (
            <span
              key={w.wordId}
              title={w.translation}
              style={{
                fontSize: 12,
                fontWeight: 700,
                padding: '3px 10px',
                borderRadius: 'var(--r-pill)',
                border: '1.5px solid var(--line-2)',
                background: 'var(--bg-1)',
                color: 'var(--fg-3)',
              }}
            >
              {w.term}
            </span>
          ))}
        </div>
      )}

      {/* Transcript */}
      <div
        role="log"
        aria-label={detail.title}
        style={{
          display: 'flex',
          flexDirection: 'column',
          gap: 12,
          padding: '16px clamp(8px, 3vw, 20px)',
          background: 'var(--bg-2)',
          border: '1px solid var(--line-2)',
          borderRadius: 'var(--r-lg)',
        }}
      >
        {detail.messages.map((m) => (
          <div
            key={m.id}
            style={{
              alignSelf: m.role === 'assistant' ? 'flex-start' : 'flex-end',
              maxWidth: '78%',
              padding: '10px 14px',
              borderRadius: 'var(--r-lg)',
              background: m.role === 'assistant' ? 'var(--bg-1)' : 'var(--accent-color)',
              border: m.role === 'assistant' ? '1px solid var(--line-2)' : 'none',
              color: m.role === 'assistant' ? 'var(--fg-1)' : '#fff',
              fontSize: 14,
              lineHeight: 1.5,
              whiteSpace: 'pre-wrap',
              overflowWrap: 'anywhere',
            }}
          >
            {m.content}
          </div>
        ))}
      </div>
    </div>
  )
}
