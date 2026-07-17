import { useQuery } from '@tanstack/react-query'
import { apiClient } from '@/shared/api'
import { env } from '@/shared/config'

export interface TtsCapabilities {
  enabled: boolean
  /** 2-letter language codes that have a configured server voice. */
  languages: string[]
}

/**
 * Server TTS availability, fetched once and cached. When the deploy flag is off the query is disabled
 * and callers treat TTS as unavailable (browser speech only).
 */
export function useTtsCapabilities() {
  return useQuery({
    queryKey: ['tts', 'capabilities'],
    queryFn: () => apiClient.get<TtsCapabilities>('/api/tts/capabilities').then((r) => r.data),
    staleTime: 5 * 60_000,
    enabled: env.TTS_ENABLED,
  })
}
