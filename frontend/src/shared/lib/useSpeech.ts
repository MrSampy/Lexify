import { useCallback, useMemo, useSyncExternalStore } from 'react'
import { LANGUAGES } from '@/shared/config'

const synth = typeof window !== 'undefined' ? window.speechSynthesis : undefined

// Voices load asynchronously in Chrome: getVoices() is empty until 'voiceschanged' fires,
// so keep a module-level snapshot and let components re-render when it updates.
let voicesSnapshot: SpeechSynthesisVoice[] = synth?.getVoices() ?? []
const listeners = new Set<() => void>()

synth?.addEventListener('voiceschanged', () => {
  voicesSnapshot = synth.getVoices()
  listeners.forEach((notify) => notify())
})

function subscribe(notify: () => void) {
  listeners.add(notify)
  return () => listeners.delete(notify)
}

function getSnapshot() {
  return voicesSnapshot
}

function pickVoice(
  voices: SpeechSynthesisVoice[],
  locale: string | undefined,
): SpeechSynthesisVoice | undefined {
  if (!locale || voices.length === 0) return undefined

  const normalize = (lang: string) => lang.replace('_', '-').toLowerCase()
  const target = normalize(locale)
  const primary = target.split('-')[0]

  return (
    voices.find((v) => normalize(v.lang) === target) ??
    voices.find((v) => normalize(v.lang).startsWith(primary)) ??
    // Norwegian voices may be tagged either nb-* (Bokmål) or no-*
    (primary === 'nb' ? voices.find((v) => normalize(v.lang).startsWith('no')) : undefined)
  )
}

/**
 * Text-to-speech for a Lexify language. `supported` is false (and `speak` a no-op)
 * when the browser has no voice for the language — hide the trigger in that case.
 */
export function useSpeech(languageId?: number | null) {
  const voices = useSyncExternalStore(subscribe, getSnapshot)
  const locale = languageId != null ? LANGUAGES[languageId]?.speechLocale : undefined
  const voice = useMemo(() => pickVoice(voices, locale), [voices, locale])

  const speak = useCallback(
    (text: string) => {
      if (!synth || !voice || !text.trim()) return
      synth.cancel() // stop any previous utterance before starting a new one
      const utterance = new SpeechSynthesisUtterance(text)
      utterance.voice = voice
      utterance.lang = voice.lang
      synth.speak(utterance)
    },
    [voice],
  )

  return { supported: voice !== undefined, speak }
}
