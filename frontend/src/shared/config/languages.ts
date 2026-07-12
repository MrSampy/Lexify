// speechLocale: preferred BCP-47 locale for Web Speech API voice selection (TTS).
export const LANGUAGES: Record<number, { code: string; name: string; speechLocale: string }> = {
  1: { code: 'en', name: 'English', speechLocale: 'en-US' },
  2: { code: 'no', name: 'Norwegian', speechLocale: 'nb-NO' },
  3: { code: 'uk', name: 'Ukrainian', speechLocale: 'uk-UA' },
  4: { code: 'ru', name: 'Russian', speechLocale: 'ru-RU' },
  5: { code: 'de', name: 'German', speechLocale: 'de-DE' },
  6: { code: 'pl', name: 'Polish', speechLocale: 'pl-PL' },
  7: { code: 'fr', name: 'French', speechLocale: 'fr-FR' },
  8: { code: 'es', name: 'Spanish', speechLocale: 'es-ES' },
  9: { code: 'it', name: 'Italian', speechLocale: 'it-IT' },
}
