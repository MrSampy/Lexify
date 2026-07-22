import { useAuthStore } from '@/entities/user'
import { env } from '@/shared/config/env'

export type ChatSseEventType = 'streaming' | 'done' | 'error'

export interface ChatSseEvent {
  type: ChatSseEventType
  chunk?: string
  message?: string
}

interface StreamChatOptions {
  conversationId: string
  message: string
  nativeLanguage: string
  onEvent: (event: ChatSseEvent) => void
  signal?: AbortSignal
}

/**
 * Sends a learner message and consumes Lexi's streamed reply over SSE — the same POST-body SSE shape as
 * the word-import formatter (see streamFormatWords). Emits `streaming` chunks, then `done` or `error`.
 */
export async function streamChatMessage({
  conversationId,
  message,
  nativeLanguage,
  onEvent,
  signal,
}: StreamChatOptions): Promise<void> {
  const token = useAuthStore.getState().accessToken

  const res = await fetch(`${env.API_URL}/api/conversations/${conversationId}/messages`, {
    method: 'POST',
    signal,
    credentials: 'include',
    headers: {
      'Content-Type': 'application/json',
      Accept: 'text/event-stream',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    body: JSON.stringify({ message, nativeLanguage }),
  })

  if (!res.ok || !res.body) {
    throw new Error(`Chat request failed: ${res.status} ${res.statusText}`)
  }

  const reader = res.body.getReader()
  const decoder = new TextDecoder()
  let buffer = ''
  let pendingEventType: string | null = null

  while (true) {
    const { done, value } = await reader.read()
    if (done) break

    buffer += decoder.decode(value, { stream: true })
    const lines = buffer.split('\n')
    buffer = lines.pop() ?? ''

    for (const line of lines) {
      if (line.startsWith('event:')) {
        pendingEventType = line.slice(6).trim()
      } else if (line.startsWith('data:') && pendingEventType) {
        const raw = line.slice(5).trim()
        let parsed: Record<string, unknown> | null = null
        try {
          parsed = JSON.parse(raw) as Record<string, unknown>
        } catch {
          // ignore malformed JSON chunks
        }
        if (parsed) {
          const event: ChatSseEvent = { type: pendingEventType as ChatSseEventType }
          if (pendingEventType === 'streaming') event.chunk = parsed.chunk as string
          else if (pendingEventType === 'error') event.message = parsed.message as string
          onEvent(event)
        }
        pendingEventType = null
      }
    }
  }
}
