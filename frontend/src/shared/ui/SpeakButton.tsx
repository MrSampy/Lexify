import { Volume2 } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { useSpeech } from '@/shared/lib/useSpeech'

interface SpeakButtonProps {
  text: string
  languageId?: number | null
  size?: number
  style?: React.CSSProperties
}

/** Pronounces `text` with a browser voice for the given language; renders nothing when no voice exists. */
export function SpeakButton({ text, languageId, size = 15, style }: SpeakButtonProps) {
  const { t } = useTranslation()
  const { supported, speak } = useSpeech(languageId)

  if (!supported) return null

  return (
    <button
      type="button"
      aria-label={t('common.speak')}
      title={t('common.speak')}
      onClick={(e) => {
        e.stopPropagation()
        speak(text)
      }}
      className="cursor-pointer border-none bg-transparent p-0 text-[var(--fg-4)] transition-colors duration-100 hover:text-[var(--accent-color)]"
      style={{ display: 'inline-flex', alignItems: 'center', ...style }}
    >
      <Volume2 style={{ width: size, height: size }} />
    </button>
  )
}
