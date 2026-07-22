import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Mascot, SpeakButton, Spinner } from '@/shared/ui'
import { useSpeak } from '@/shared/lib'
import {
  useEndConversationMutation,
  type ChatMessage,
  type ConversationSummary,
  type ConversationTargetWord,
} from '@/entities/conversation'
import { streamChatMessage } from '../api/streamChat'
import { usedWordIds, livePoints } from '../lib/scoring'

const AUTO_SPEAK_KEY = 'lexify_chat_autospeak'
// Server-side cap in SendConversationMessageCommandHandler — enforced here too so the learner
// hits a hard stop while typing instead of a non-localized server error on send.
const MAX_MESSAGE_LENGTH = 2000

interface ChatSessionProps {
  conversationId: string
  languageId: number
  targetWords: ConversationTargetWord[]
  initialMessages: ChatMessage[]
  messageBudget: number
  onEnded: (summary: ConversationSummary) => void
}

export function ChatSession({
  conversationId,
  languageId,
  targetWords,
  initialMessages,
  messageBudget,
  onEnded,
}: ChatSessionProps) {
  const { t, i18n } = useTranslation()
  const nativeLanguage = i18n.resolvedLanguage === 'uk' ? 'Ukrainian' : 'English'

  const [messages, setMessages] = useState<ChatMessage[]>(initialMessages)
  const [input, setInput] = useState('')
  const [streamingText, setStreamingText] = useState('')
  const [isStreaming, setIsStreaming] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [autoSpeak, setAutoSpeak] = useState(() => localStorage.getItem(AUTO_SPEAK_KEY) === '1')

  const abortRef = useRef<AbortController | null>(null)
  const scrollRef = useRef<HTMLDivElement | null>(null)

  const endConversation = useEndConversationMutation()

  // Reply-language TTS. With no wordId, useSpeak synthesizes the free-form reply text via Piper
  // (POST /api/tts/speak), falling back to browser speech only when server TTS is unavailable.
  const { speak, ready: speakReady } = useSpeak({ wordId: null, languageId })

  const learnerMessages = useMemo(
    () => messages.filter((m) => m.role === 'user').map((m) => m.content),
    [messages],
  )

  // Live challenge state (authoritative final score comes from the server summary). Detection mirrors
  // the backend's normalization so the chips agree with the end screen.
  const usedTerms = useMemo(
    () => usedWordIds(targetWords, learnerMessages),
    [targetWords, learnerMessages],
  )
  const messagesUsed = learnerMessages.length
  const messagesLeft = Math.max(0, messageBudget - messagesUsed)
  const atBudget = messagesUsed >= messageBudget
  const points = useMemo(
    () => livePoints(usedTerms.size, targetWords, learnerMessages),
    [usedTerms, targetWords, learnerMessages],
  )

  useEffect(() => {
    scrollRef.current?.scrollTo({ top: scrollRef.current.scrollHeight, behavior: 'smooth' })
  }, [messages, streamingText])

  useEffect(() => () => abortRef.current?.abort(), [])

  const toggleAutoSpeak = () => {
    setAutoSpeak((prev) => {
      const next = !prev
      localStorage.setItem(AUTO_SPEAK_KEY, next ? '1' : '0')
      return next
    })
  }

  const send = useCallback(async () => {
    const text = input.trim()
    if (!text || isStreaming || atBudget) return

    setError(null)
    setInput('')
    const userMessageId = crypto.randomUUID()
    setMessages((prev) => [
      ...prev,
      { id: userMessageId, role: 'user', content: text, createdAt: new Date().toISOString() },
    ])
    setIsStreaming(true)
    setStreamingText('')
    abortRef.current = new AbortController()

    let accumulated = ''
    let failed = false
    try {
      await streamChatMessage({
        conversationId,
        message: text,
        nativeLanguage,
        signal: abortRef.current.signal,
        onEvent: (evt) => {
          if (evt.type === 'streaming' && evt.chunk) {
            accumulated += evt.chunk
            setStreamingText(accumulated)
          } else if (evt.type === 'error') {
            failed = true
            setError(evt.message ?? t('chat.errReply'))
          }
        },
      })
    } catch (err) {
      if (err instanceof Error && err.name !== 'AbortError') {
        failed = true
        setError(t('chat.errConnection'))
      }
    } finally {
      setIsStreaming(false)
      setStreamingText('')
      if (accumulated.trim()) {
        setMessages((prev) => [
          ...prev,
          {
            id: crypto.randomUUID(),
            role: 'assistant',
            content: accumulated.trim(),
            createdAt: new Date().toISOString(),
          },
        ])
        if (autoSpeak && speakReady) void speak(accumulated.trim())
      } else if (failed) {
        // Nothing streamed back: roll back the optimistic bubble (frees the budget slot in the HUD)
        // and restore the draft so the learner can resend without retyping.
        setMessages((prev) => prev.filter((m) => m.id !== userMessageId))
        setInput((current) => current || text)
      }
    }
  }, [
    input,
    isStreaming,
    atBudget,
    conversationId,
    nativeLanguage,
    autoSpeak,
    speakReady,
    speak,
    t,
  ])

  const handleEnd = async () => {
    abortRef.current?.abort()
    try {
      const summary = await endConversation.mutateAsync(conversationId)
      onEnded(summary)
    } catch {
      setError(t('chat.errEnd'))
    }
  }

  return (
    <div style={{ display: 'flex', flexDirection: 'column', height: 'min(72vh, 720px)' }}>
      {/* Challenge HUD */}
      <div
        style={{
          display: 'flex',
          flexWrap: 'wrap',
          gap: 8,
          padding: '0 2px 10px',
          alignItems: 'center',
        }}
      >
        <HudStat label={t('chat.hudWords')} value={`${usedTerms.size}/${targetWords.length}`} />
        <HudStat
          label={t('chat.hudMessagesLeft')}
          value={String(messagesLeft)}
          warn={messagesLeft <= 1}
        />
        <HudStat label={t('chat.hudPoints')} value={String(points)} accent />
      </div>

      {/* Target-word chips */}
      <div
        style={{
          display: 'flex',
          flexWrap: 'wrap',
          gap: 6,
          padding: '0 2px 12px',
          alignItems: 'center',
        }}
      >
        <span style={{ fontSize: 12, fontWeight: 700, color: 'var(--fg-4)' }}>
          {t('chat.wordsToUse')}
        </span>
        {targetWords.map((w) => {
          const used = usedTerms.has(w.wordId)
          return (
            <span
              key={w.wordId}
              title={w.translation}
              style={{
                fontSize: 12,
                fontWeight: 700,
                padding: '3px 10px',
                borderRadius: 'var(--r-pill)',
                border: `1.5px solid ${used ? 'var(--success)' : 'var(--line-2)'}`,
                background: used ? 'var(--success-ghost, var(--accent-ghost))' : 'var(--bg-1)',
                color: used ? 'var(--success)' : 'var(--fg-3)',
              }}
            >
              {used ? '✓ ' : ''}
              {w.term}
            </span>
          )
        })}
      </div>

      {/* Transcript */}
      <div
        ref={scrollRef}
        role="log"
        aria-label={t('chat.title')}
        style={{
          flex: 1,
          overflowY: 'auto',
          display: 'flex',
          flexDirection: 'column',
          gap: 12,
          padding: '12px clamp(8px, 3vw, 20px)',
          background: 'var(--bg-2)',
          border: '1px solid var(--line-2)',
          borderRadius: 'var(--r-lg)',
        }}
      >
        {messages.map((m) => (
          <MessageBubble key={m.id} message={m} languageId={languageId} />
        ))}

        {isStreaming && (
          <div aria-live="polite" style={{ display: 'flex', alignItems: 'flex-end', gap: 8 }}>
            <Mascot pose="diving" size={40} />
            <div
              style={{
                maxWidth: '78%',
                padding: '10px 14px',
                borderRadius: 'var(--r-lg)',
                background: 'var(--bg-1)',
                border: '1px solid var(--line-2)',
                color: 'var(--fg-2)',
                fontSize: 14,
                lineHeight: 1.5,
              }}
            >
              {streamingText || <Spinner size="sm" />}
            </div>
          </div>
        )}
      </div>

      {error && (
        <div
          style={{ color: 'var(--danger)', fontSize: 13, fontWeight: 600, padding: '8px 2px 0' }}
        >
          {error}
        </div>
      )}

      {atBudget && !error && (
        <div
          style={{
            color: 'var(--accent-dim)',
            fontSize: 13,
            fontWeight: 600,
            padding: '8px 2px 0',
          }}
        >
          {t('chat.budgetReached')}
        </div>
      )}

      {/* Composer */}
      <div style={{ display: 'flex', gap: 8, alignItems: 'flex-end', paddingTop: 12 }}>
        <button
          onClick={toggleAutoSpeak}
          aria-pressed={autoSpeak}
          title={t('chat.autoSpeak')}
          style={{
            flexShrink: 0,
            border: '1.5px solid var(--line-2)',
            background: autoSpeak ? 'var(--accent-ghost)' : 'var(--bg-1)',
            color: autoSpeak ? 'var(--accent-dim)' : 'var(--fg-3)',
            fontSize: 16,
            padding: '9px 12px',
            borderRadius: 'var(--r-md)',
            cursor: 'pointer',
          }}
        >
          🔊
        </button>
        <textarea
          value={input}
          onChange={(e) => setInput(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === 'Enter' && !e.shiftKey) {
              e.preventDefault()
              void send()
            }
          }}
          placeholder={atBudget ? t('chat.budgetReachedShort') : t('chat.inputPlaceholder')}
          aria-label={t('chat.composerLabel')}
          rows={1}
          maxLength={MAX_MESSAGE_LENGTH}
          disabled={atBudget}
          style={{
            flex: 1,
            resize: 'none',
            minHeight: 42,
            maxHeight: 140,
            padding: '10px 14px',
            borderRadius: 'var(--r-md)',
            border: '1.5px solid var(--line-2)',
            background: atBudget ? 'var(--bg-2)' : 'var(--bg-1)',
            color: 'var(--fg-1)',
            fontSize: 14,
            fontFamily: 'var(--font-body)',
          }}
        />
        <button
          className="lx-btn-primary"
          onClick={() => void send()}
          disabled={isStreaming || atBudget || input.trim().length === 0}
          style={{ flexShrink: 0 }}
        >
          {t('chat.send')}
        </button>
        <button
          className={atBudget ? 'lx-btn-primary' : 'lx-btn-secondary'}
          onClick={() => void handleEnd()}
          disabled={endConversation.isPending}
          style={{ flexShrink: 0 }}
        >
          {endConversation.isPending ? t('chat.ending') : t('chat.finish')}
        </button>
      </div>
    </div>
  )
}

function HudStat({
  label,
  value,
  accent,
  warn,
}: {
  label: string
  value: string
  accent?: boolean
  warn?: boolean
}) {
  const color = warn ? 'var(--warning)' : accent ? 'var(--accent-color)' : 'var(--fg-2)'
  return (
    <span
      style={{
        display: 'inline-flex',
        alignItems: 'baseline',
        gap: 6,
        padding: '4px 12px',
        borderRadius: 'var(--r-pill)',
        border: '1.5px solid var(--line-2)',
        background: 'var(--bg-1)',
      }}
    >
      <span style={{ fontSize: 11, fontWeight: 700, color: 'var(--fg-4)' }}>{label}</span>
      <span style={{ fontSize: 14, fontWeight: 800, color }}>{value}</span>
    </span>
  )
}

function MessageBubble({ message, languageId }: { message: ChatMessage; languageId: number }) {
  const isAssistant = message.role === 'assistant'
  return (
    <div
      style={{
        display: 'flex',
        alignItems: 'flex-end',
        gap: 8,
        flexDirection: isAssistant ? 'row' : 'row-reverse',
      }}
    >
      {isAssistant && <Mascot pose="greeting" size={40} />}
      <div
        style={{
          maxWidth: '78%',
          padding: '10px 14px',
          borderRadius: 'var(--r-lg)',
          background: isAssistant ? 'var(--bg-1)' : 'var(--accent-color)',
          border: isAssistant ? '1px solid var(--line-2)' : 'none',
          color: isAssistant ? 'var(--fg-1)' : '#fff',
          fontSize: 14,
          lineHeight: 1.5,
          whiteSpace: 'pre-wrap',
          overflowWrap: 'anywhere',
        }}
      >
        {message.content}
        {isAssistant && (
          <div style={{ marginTop: 4 }}>
            <SpeakButton text={message.content} languageId={languageId} size={16} />
          </div>
        )}
      </div>
    </div>
  )
}
