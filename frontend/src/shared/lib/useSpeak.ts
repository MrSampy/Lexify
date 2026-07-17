import { useCallback, useRef, useState } from 'react'
import { apiClient } from '@/shared/api'
import { env, LANGUAGES } from '@/shared/config'
import { useSpeech } from './useSpeech'
import { useTtsCapabilities } from './useTtsCapabilities'

// Session-wide cache of object URLs per word, so repeat plays don't refetch. Not revoked: audio clips
// are tiny and live only for the tab session.
const audioUrlCache = new Map<string, string>()

interface UseSpeakArgs {
  wordId?: string | null
  languageId?: number | null
}

/**
 * Speaks a word. Prefers server-side neural audio (Piper) when available for the word's language,
 * and transparently falls back to browser speech synthesis on any miss (feature off, unsupported
 * language, or network/HTTP error). `supported` is true if either path can speak.
 */
export function useSpeak({ wordId, languageId }: UseSpeakArgs) {
  const { supported: browserSupported, speak: browserSpeak } = useSpeech(languageId)
  const { data: caps } = useTtsCapabilities()
  const [isLoading, setIsLoading] = useState(false)
  const [isPlaying, setIsPlaying] = useState(false)
  const audioRef = useRef<HTMLAudioElement | null>(null)

  const langCode = languageId != null ? LANGUAGES[languageId]?.code : undefined
  const serverAvailable =
    env.TTS_ENABLED &&
    !!wordId &&
    caps?.enabled === true &&
    !!langCode &&
    caps.languages.includes(langCode)

  const speak = useCallback(
    async (text: string) => {
      if (serverAvailable && wordId) {
        try {
          setIsLoading(true)
          let url = audioUrlCache.get(wordId)
          if (!url) {
            const res = await apiClient.get(`/api/tts/word/${wordId}`, { responseType: 'blob' })
            url = URL.createObjectURL(res.data as Blob)
            audioUrlCache.set(wordId, url)
          }
          audioRef.current?.pause()
          const audio = new Audio(url)
          audioRef.current = audio
          audio.onplay = () => setIsPlaying(true)
          audio.onended = () => setIsPlaying(false)
          audio.onerror = () => setIsPlaying(false)
          await audio.play()
          setIsLoading(false)
          return
        } catch {
          setIsLoading(false)
          // Any failure (404 = off/not owned/unsupported, or network) → fall through to browser TTS.
        }
      }
      browserSpeak(text)
    },
    [serverAvailable, wordId, browserSpeak],
  )

  return { speak, isLoading, isPlaying, supported: serverAvailable || browserSupported }
}
