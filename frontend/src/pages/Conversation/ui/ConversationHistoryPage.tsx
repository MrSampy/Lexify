import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { ROUTES } from '@/shared/config'
import { Mascot, Spinner } from '@/shared/ui'
import { useConversations, type ConversationListItem } from '@/entities/conversation'

export function ConversationHistoryPage() {
  const { t } = useTranslation()
  const [page, setPage] = useState(1)
  const { data, isLoading } = useConversations(page)

  return (
    <div style={{ maxWidth: 720, margin: '0 auto' }}>
      <div
        style={{
          display: 'flex',
          alignItems: 'baseline',
          justifyContent: 'space-between',
          marginBottom: 16,
        }}
      >
        <h1 className="ds-h2" style={{ margin: 0 }}>
          {t('chat.historyTitle')}
        </h1>
        <Link
          to={ROUTES.PRACTICE_CHAT}
          style={{
            color: 'var(--accent-color)',
            textDecoration: 'none',
            fontSize: 14,
            fontWeight: 700,
          }}
        >
          {t('chat.newChat')}
        </Link>
      </div>

      {isLoading ? (
        <div style={{ display: 'flex', justifyContent: 'center', padding: '60px 0' }}>
          <Spinner size="lg" />
        </div>
      ) : !data || data.items.length === 0 ? (
        <div className="flex min-h-[40vh] flex-col items-center justify-center gap-4 text-center">
          <Mascot pose="lost" size={110} />
          <p className="ds-body text-[var(--fg-3)]">{t('chat.historyEmpty')}</p>
          <Link to={ROUTES.PRACTICE_CHAT} className="no-underline">
            <button className="lx-btn-primary">{t('chat.start')}</button>
          </Link>
        </div>
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
          {data.items.map((item) => (
            <HistoryRow key={item.id} item={item} />
          ))}
        </div>
      )}

      {data && data.totalPages > 1 && (
        <div
          style={{
            marginTop: 24,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            gap: 12,
          }}
        >
          <button
            className="lx-btn-secondary"
            disabled={!data.hasPreviousPage}
            onClick={() => setPage((p) => p - 1)}
            style={{ padding: '8px 16px' }}
          >
            {t('common.previous')}
          </button>
          <span className="ds-code" style={{ color: 'var(--fg-3)' }}>
            {page} / {data.totalPages}
          </span>
          <button
            className="lx-btn-secondary"
            disabled={!data.hasNextPage}
            onClick={() => setPage((p) => p + 1)}
            style={{ padding: '8px 16px' }}
          >
            {t('common.next')}
          </button>
        </div>
      )}
    </div>
  )
}

function HistoryRow({ item }: { item: ConversationListItem }) {
  const { t, i18n } = useTranslation()
  const active = item.status === 'active'
  const date = new Date(item.createdAt).toLocaleDateString(i18n.language, {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
  })

  return (
    <Link
      to={ROUTES.PRACTICE_CHAT_SESSION(item.id)}
      className="lx-card no-underline"
      style={{
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        gap: 12,
        padding: '14px 18px',
      }}
    >
      <div style={{ minWidth: 0 }}>
        <div
          style={{
            fontWeight: 700,
            fontSize: 15,
            color: 'var(--fg-1)',
            overflow: 'hidden',
            textOverflow: 'ellipsis',
            whiteSpace: 'nowrap',
          }}
        >
          {item.title}
        </div>
        <div className="ds-sm" style={{ color: 'var(--fg-4)', marginTop: 2 }}>
          {date} · {t('chat.messagesCount', { count: item.messageCount })}
        </div>
      </div>

      <div style={{ display: 'flex', alignItems: 'center', gap: 12, flexShrink: 0 }}>
        {active ? (
          <span
            style={{
              fontSize: 12,
              fontWeight: 700,
              padding: '3px 10px',
              borderRadius: 'var(--r-pill)',
              border: '1.5px solid var(--accent-color)',
              color: 'var(--accent-dim)',
              background: 'var(--accent-ghost)',
            }}
          >
            {t('chat.resume')}
          </span>
        ) : item.stars != null ? (
          <span
            aria-label={`${item.stars}/3`}
            style={{ fontSize: 16, letterSpacing: 2, whiteSpace: 'nowrap' }}
          >
            {[1, 2, 3].map((n) => (
              <span
                key={n}
                style={{ color: n <= (item.stars ?? 0) ? 'var(--warning)' : 'var(--line-2)' }}
              >
                ★
              </span>
            ))}
          </span>
        ) : (
          <span style={{ color: 'var(--fg-4)', fontSize: 14 }}>—</span>
        )}
      </div>
    </Link>
  )
}
