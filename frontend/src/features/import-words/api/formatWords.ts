import { useAuthStore } from '@/entities/user'
import { env } from '@/shared/config/env'
import type { FormatWordsResult } from '../model/types'

export type SseEventType = 'parsing' | 'streaming' | 'done' | 'error'

export interface FormattingSseEvent {
  type: SseEventType
  chunk?: string
  result?: FormatWordsResult
  message?: string
}

interface StreamFormatWordsOptions {
  rawText: string
  targetLanguage: string
  nativeLanguage: string
  onEvent: (event: FormattingSseEvent) => void
  signal?: AbortSignal
}

export async function streamFormatWords({
  rawText,
  targetLanguage,
  nativeLanguage,
  onEvent,
  signal,
}: StreamFormatWordsOptions): Promise<void> {
  const token = useAuthStore.getState().accessToken

  const res = await fetch(`${env.API_URL}/api/words/format`, {
    method: 'POST',
    signal,
    credentials: 'include',
    headers: {
      'Content-Type': 'application/json',
      Accept: 'text/event-stream',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    body: JSON.stringify({ rawText, targetLanguage, nativeLanguage }),
  })

  if (!res.ok || !res.body) {
    throw new Error(`Format request failed: ${res.status} ${res.statusText}`)
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
        let parsedData: Record<string, unknown> | null = null
        try {
          parsedData = JSON.parse(raw) as Record<string, unknown>
        } catch {
          // ignore malformed JSON chunks
        }
        if (parsedData) {
          const event: FormattingSseEvent = { type: pendingEventType as SseEventType }

          if (pendingEventType === 'streaming') {
            event.chunk = parsedData.chunk as string
          } else if (pendingEventType === 'done') {
            event.result = parsedData.result as FormatWordsResult
          } else if (pendingEventType === 'error') {
            event.message = parsedData.message as string
          }

          onEvent(event)
        }

        pendingEventType = null
      }
    }
  }
}
