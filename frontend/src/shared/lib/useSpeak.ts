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
  const { data: caps, isFetched: capsFetched } = useTtsCapabilities()

  // The server-vs-browser decision is final only once the capabilities query settles (or when
  // server TTS is disabled outright). Callers that auto-play must wait for this, otherwise they
  // speak via the browser first and then again via Piper when caps arrive — two overlapping voices.
  const ready = !env.TTS_ENABLED || capsFetched
  const [isLoading, setIsLoading] = useState(false)
  const [isPlaying, setIsPlaying] = useState(false)
  const audioRef = useRef<HTMLAudioElement | null>(null)

  const langCode = languageId != null ? LANGUAGES[languageId]?.code : undefined
  // Server audio no longer requires a wordId: with a wordId we fetch the stored word's clip, without one
  // we synthesize arbitrary text (e.g. Lexi's chat replies) via POST /api/tts/speak. Either way it needs
  // the feature on and a Piper voice for the language.
  const serverAvailable =
    env.TTS_ENABLED && caps?.enabled === true && !!langCode && caps.languages.includes(langCode)

  const speak = useCallback(
    async (text: string) => {
      if (serverAvailable && langCode) {
        // Cache by wordId when present, otherwise by language+text so repeat replies don't re-synthesize.
        const cacheKey = wordId ?? `${langCode}|${text}`
        let gotAudio = false
        try {
          setIsLoading(true)
          let url = audioUrlCache.get(cacheKey)
          if (!url) {
            const res = wordId
              ? await apiClient.get(`/api/tts/word/${wordId}`, { responseType: 'blob' })
              : await apiClient.post(
                  '/api/tts/speak',
                  { text, languageCode: langCode },
                  { responseType: 'blob' },
                )
            url = URL.createObjectURL(res.data as Blob)
            audioUrlCache.set(cacheKey, url)
          }
          audioRef.current?.pause()
          const audio = new Audio(url)
          audioRef.current = audio
          audio.onplay = () => setIsPlaying(true)
          audio.onended = () => setIsPlaying(false)
          audio.onerror = () => setIsPlaying(false)
          gotAudio = true
          setIsLoading(false)
          await audio.play()
          return
        } catch {
          setIsLoading(false)
          // If we obtained the server clip but playback was refused (e.g. autoplay policy on a
          // gesture-less auto-play), DON'T also speak via the browser — stacking Piper + browser
          // voices is the double-audio bug. Only fall through when we never got server audio at all
          // (404 = off/not owned/unsupported, or a network error before the clip loaded).
          if (gotAudio) return
        }
      }
      browserSpeak(text)
    },
    [serverAvailable, wordId, langCode, browserSpeak],
  )

  return { speak, isLoading, isPlaying, ready, supported: serverAvailable || browserSupported }
}
