import { env } from '@/shared/config/env'
import { getAuthHandlers } from './base'

export interface RawSseEvent {
  event: string
  data: Record<string, unknown>
}

/**
 * POST-body SSE fetch with the same injected auth as apiClient: Bearer from the auth store, and on a
 * 401 one refresh + retry (mirrors the axios interceptor — raw fetch bypasses it, so the stream
 * endpoints must self-heal an expired access token the same way every other call does).
 */
export async function sseFetch(
  path: string,
  body: object,
  signal?: AbortSignal,
): Promise<Response> {
  const doFetch = () => {
    const token = getAuthHandlers()?.getToken()
    return fetch(`${env.API_URL}${path}`, {
      method: 'POST',
      signal,
      credentials: 'include',
      headers: {
        'Content-Type': 'application/json',
        Accept: 'text/event-stream',
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
      body: JSON.stringify(body),
    })
  }

  let res = await doFetch()
  if (res.status === 401) {
    const handlers = getAuthHandlers()
    if (handlers && (await handlers.refresh())) {
      res = await doFetch()
    } else {
      handlers?.logout()
    }
  }
  if (!res.ok || !res.body) {
    throw new Error(`SSE request failed: ${res.status} ${res.statusText}`)
  }
  return res
}

/**
 * Reads a named-event SSE stream (`event: x\ndata: {json}`): buffers partial chunks across reads,
 * skips malformed JSON, and invokes onEvent once per complete event.
 */
export async function readSseEvents(
  body: ReadableStream<Uint8Array>,
  onEvent: (event: RawSseEvent) => void,
): Promise<void> {
  const reader = body.getReader()
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
        if (parsed) onEvent({ event: pendingEventType, data: parsed })
        pendingEventType = null
      }
    }
  }
}
