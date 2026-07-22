import { readSseEvents, sseFetch } from '@/shared/api'
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
  const res = await sseFetch(
    '/api/words/format',
    { rawText, targetLanguage, nativeLanguage },
    signal,
  )

  await readSseEvents(res.body!, ({ event, data }) => {
    const evt: FormattingSseEvent = { type: event as SseEventType }
    if (event === 'streaming') evt.chunk = data.chunk as string
    else if (event === 'done') evt.result = data.result as FormatWordsResult
    else if (event === 'error') evt.message = data.message as string
    onEvent(evt)
  })
}
