import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useLocation, useParams } from 'react-router-dom'
import { ROUTES } from '@/shared/config'
import { Mascot, Spinner } from '@/shared/ui'
import { ChatSession, budgetFor } from '@/features/chat-with-lexi'
import {
  useConversation,
  type ChatMessage,
  type ConversationSummary,
  type ConversationTargetWord,
  type StartConversationResult,
} from '@/entities/conversation'

export function ConversationPage() {
  const { t } = useTranslation()
  const { id } = useParams<{ id: string }>()
  const location = useLocation()
  const start = (location.state as { start?: StartConversationResult } | null)?.start

  const [summary, setSummary] = useState<ConversationSummary | null>(null)

  // Fetch the transcript only when we didn't arrive with the fresh start payload (e.g. reload/resume).
  const { data: detail, isLoading } = useConversation(start ? '' : (id ?? ''))

  if (summary) {
    return <ConversationSummaryView summary={summary} />
  }

  if (!id) {
    return <Redirect />
  }

  let languageId: number
  let targetWords: ConversationTargetWord[]
  let initialMessages: ChatMessage[]
  let messageBudget: number
  let ended = false

  if (start) {
    languageId = start.languageId
    targetWords = start.targetWords
    messageBudget = start.messageBudget
    initialMessages = [
      {
        id: crypto.randomUUID(),
        role: 'assistant',
        content: start.openingMessage,
        createdAt: new Date().toISOString(),
      },
    ]
  } else if (detail) {
    languageId = detail.languageId
    targetWords = detail.targetWords
    messageBudget = budgetFor(detail.targetWords.length)
    initialMessages = detail.messages
    ended = detail.status === 'ended'
  } else if (isLoading) {
    return (
      <div style={{ display: 'flex', justifyContent: 'center', padding: '80px 0' }}>
        <Spinner size="lg" />
      </div>
    )
  } else {
    return <Redirect />
  }

  return (
    <div style={{ maxWidth: 820, margin: '0 auto' }}>
      <div
        style={{
          display: 'flex',
          alignItems: 'baseline',
          justifyContent: 'space-between',
          marginBottom: 16,
        }}
      >
        <h1 className="ds-h3" style={{ margin: 0 }}>
          {t('chat.title')}
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

      {ended ? (
        <ReadOnlyTranscript messages={initialMessages} />
      ) : (
        <ChatSession
          conversationId={id}
          languageId={languageId}
          targetWords={targetWords}
          initialMessages={initialMessages}
          messageBudget={messageBudget}
          onEnded={setSummary}
        />
      )}
    </div>
  )
}

function Redirect() {
  const { t } = useTranslation()
  return (
    <div className="flex min-h-[50vh] flex-col items-center justify-center gap-4 text-center">
      <Mascot pose="lost" size={120} />
      <p className="ds-body text-[var(--fg-3)]">{t('chat.notFound')}</p>
      <Link to={ROUTES.PRACTICE_CHAT} className="no-underline">
        <button className="lx-btn-primary">{t('chat.newChat')}</button>
      </Link>
    </div>
  )
}

function ReadOnlyTranscript({ messages }: { messages: ChatMessage[] }) {
  return (
    <div
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
      {messages.map((m) => (
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
  )
}

function ConversationSummaryView({ summary }: { summary: ConversationSummary }) {
  const { t } = useTranslation()
  const { score } = summary

  return (
    <div className="flex min-h-[60vh] flex-col items-center justify-center gap-6 text-center">
      <Mascot pose="celebrate" size={140} animate />
      <div className="ds-h2">{t('chat.complete')}</div>

      {/* Stars */}
      <div
        style={{ display: 'flex', gap: 6, fontSize: 40, lineHeight: 1 }}
        aria-label={`${score.stars}/3`}
      >
        {[1, 2, 3].map((n) => (
          <span key={n} style={{ color: n <= score.stars ? 'var(--warning)' : 'var(--line-2)' }}>
            ★
          </span>
        ))}
      </div>

      <p className="ds-body text-[var(--fg-3)]">
        {t('chat.scoreSummary', {
          used: score.wordsUsed,
          total: score.totalWords,
          messages: score.messagesUsed,
          budget: score.messageBudget,
        })}
      </p>
      <div
        style={{
          fontFamily: 'var(--font-display)',
          fontWeight: 800,
          fontSize: 28,
          color: 'var(--accent-color)',
        }}
      >
        {t('chat.pointsValue', { points: score.points })}
      </div>

      <div
        style={{
          maxHeight: 280,
          overflowY: 'auto',
          width: 'min(460px, 92vw)',
          border: '1px solid var(--line-2)',
          borderRadius: 'var(--r-md)',
          padding: '10px 14px',
          textAlign: 'left',
        }}
      >
        {summary.words.map((w) => (
          <div
            key={w.wordId}
            style={{
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'space-between',
              gap: 12,
              fontSize: 13,
              padding: '5px 0',
              borderBottom: '1px solid var(--line-2)',
            }}
          >
            <span style={{ display: 'flex', alignItems: 'center', gap: 8, minWidth: 0 }}>
              <span
                aria-hidden
                style={{
                  fontSize: 14,
                  color: !w.used
                    ? 'var(--fg-4)'
                    : w.usedCorrectly
                      ? 'var(--success)'
                      : 'var(--warning)',
                }}
              >
                {!w.used ? '·' : w.usedCorrectly ? '✓' : '~'}
              </span>
              <span
                style={{
                  fontWeight: 600,
                  color: 'var(--fg-2)',
                  overflow: 'hidden',
                  textOverflow: 'ellipsis',
                  whiteSpace: 'nowrap',
                }}
              >
                {w.term}
              </span>
            </span>
            <span style={{ color: 'var(--fg-4)', flexShrink: 0 }}>
              {w.used && w.intervalDays != null
                ? t('review.nextIn', { count: w.intervalDays })
                : t('chat.notUsed')}
            </span>
          </div>
        ))}
      </div>

      <div className="flex flex-wrap items-center justify-center gap-3">
        <Link to={ROUTES.PRACTICE_CHAT} className="no-underline">
          <button className="lx-btn-secondary">{t('chat.chatAgain')}</button>
        </Link>
        <Link to={ROUTES.DASHBOARD} className="no-underline">
          <button className="lx-btn-primary">{t('review.backToDashboard')}</button>
        </Link>
      </div>
    </div>
  )
}
