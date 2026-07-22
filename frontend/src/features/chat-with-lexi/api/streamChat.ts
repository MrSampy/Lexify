import { readSseEvents, sseFetch } from '@/shared/api'

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
 * the word-import formatter. Emits `streaming` chunks, then `done` or `error`.
 */
export async function streamChatMessage({
  conversationId,
  message,
  nativeLanguage,
  onEvent,
  signal,
}: StreamChatOptions): Promise<void> {
  const res = await sseFetch(
    `/api/conversations/${conversationId}/messages`,
    { message, nativeLanguage },
    signal,
  )

  await readSseEvents(res.body!, ({ event, data }) => {
    const evt: ChatSseEvent = { type: event as ChatSseEventType }
    if (event === 'streaming') evt.chunk = data.chunk as string
    else if (event === 'error') evt.message = data.message as string
    onEvent(evt)
  })
}
