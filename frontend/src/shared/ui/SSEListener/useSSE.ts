import { useCallback, useRef, useState } from 'react'

interface UseSSEOptions {
  method?: 'GET' | 'POST'
  body?: object
  onChunk: (data: unknown) => void
  onDone: () => void
  onError: (err: Error) => void
}

interface UseSSEResult {
  start: () => void
  abort: () => void
  isStreaming: boolean
}

export function useSSE(url: string, options: UseSSEOptions): UseSSEResult {
  const { method = 'GET', body, onChunk, onDone, onError } = options
  const [isStreaming, setIsStreaming] = useState(false)
  const controllerRef = useRef<AbortController | null>(null)

  const start = useCallback(async () => {
    if (isStreaming) return

    controllerRef.current = new AbortController()
    setIsStreaming(true)

    try {
      const res = await fetch(url, {
        method,
        signal: controllerRef.current.signal,
        credentials: 'include',
        headers: { 'Content-Type': 'application/json', Accept: 'text/event-stream' },
        body: body ? JSON.stringify(body) : undefined,
      })

      if (!res.ok || !res.body) {
        throw new Error(`SSE request failed: ${res.status} ${res.statusText}`)
      }

      const reader = res.body.getReader()
      const decoder = new TextDecoder()
      let buffer = ''

      while (true) {
        const { done, value } = await reader.read()
        if (done) break

        buffer += decoder.decode(value, { stream: true })
        const lines = buffer.split('\n')
        buffer = lines.pop() ?? ''

        for (const line of lines) {
          if (line.startsWith('data:')) {
            const raw = line.slice(5).trim()
            if (raw === '[DONE]') {
              onDone()
              return
            }
            try {
              onChunk(JSON.parse(raw))
            } catch {
              onChunk(raw)
            }
          }
        }
      }

      onDone()
    } catch (err) {
      if (err instanceof Error && err.name !== 'AbortError') {
        onError(err)
      }
    } finally {
      setIsStreaming(false)
    }
  }, [url, method, body, onChunk, onDone, onError, isStreaming])

  const abort = useCallback(() => {
    controllerRef.current?.abort()
    setIsStreaming(false)
  }, [])

  return { start, abort, isStreaming }
}
