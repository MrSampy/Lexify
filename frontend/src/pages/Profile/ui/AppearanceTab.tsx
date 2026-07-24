import { useTranslation } from 'react-i18next'
import { useTheme } from 'next-themes'
import { SectionCard } from './SectionCard'

const THEMES = [
  { value: 'light', labelKey: 'profile.themeLight' },
  { value: 'dark', labelKey: 'profile.themeDark' },
  { value: 'system', labelKey: 'profile.themeSystem' },
] as const

// The top bar carries compact quick-switches for both of these; this is the full-labelled home for them.
const LANGUAGES = [
  { code: 'en', label: 'English' },
  { code: 'uk', label: 'Українська' },
]

export function AppearanceTab() {
  const { t, i18n } = useTranslation()
  const { theme, setTheme } = useTheme()

  return (
    <>
      <SectionCard title={t('profile.theme')}>
        <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
          {THEMES.map(({ value, labelKey }) => (
            <button
              key={value}
              className={theme === value ? 'lx-btn-primary' : 'lx-btn-secondary'}
              aria-pressed={theme === value}
              onClick={() => setTheme(value)}
            >
              {t(labelKey)}
            </button>
          ))}
        </div>
      </SectionCard>

      <SectionCard title={t('profile.language')}>
        <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
          {LANGUAGES.map(({ code, label }) => (
            <button
              key={code}
              className={i18n.resolvedLanguage === code ? 'lx-btn-primary' : 'lx-btn-secondary'}
              aria-pressed={i18n.resolvedLanguage === code}
              onClick={() => void i18n.changeLanguage(code)}
            >
              {label}
            </button>
          ))}
        </div>
      </SectionCard>
    </>
  )
}
