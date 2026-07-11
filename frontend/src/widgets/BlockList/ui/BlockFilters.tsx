import { useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { LANGUAGES } from '@/shared/config'
import { LxSelect } from '@/shared/ui'
import { useUserTags } from '@/entities/block/api/tagApi'

interface BlockFiltersProps {
  languageId: number | undefined
  onLanguageChange: (id: number | undefined) => void
  tag: string
  onTagChange: (v: string) => void
}

export function BlockFilters({
  languageId,
  onLanguageChange,
  tag,
  onTagChange,
}: BlockFiltersProps) {
  const { t } = useTranslation()
  const [showSuggestions, setShowSuggestions] = useState(false)
  const inputRef = useRef<HTMLInputElement>(null)
  const { data: userTags = [] } = useUserTags()

  const suggestions = tag.trim()
    ? userTags.filter((t) => t.includes(tag.trim().toLowerCase())).slice(0, 6)
    : userTags.slice(0, 6)

  return (
    <div style={{ display: 'flex', flexWrap: 'wrap', gap: 10 }}>
      <div style={{ position: 'relative' }}>
        <input
          ref={inputRef}
          className="lx-input"
          placeholder={t('blocks.filterByTag')}
          value={tag}
          onChange={(e) => onTagChange(e.target.value)}
          onFocus={() => setShowSuggestions(true)}
          onBlur={() => setTimeout(() => setShowSuggestions(false), 150)}
          style={{ height: 36, width: 180, fontSize: 13 }}
        />
        {showSuggestions && suggestions.length > 0 && (
          <div
            style={{
              position: 'absolute',
              left: 0,
              top: '100%',
              zIndex: 20,
              marginTop: 4,
              width: '100%',
              background: 'var(--bg-3)',
              border: '1px solid var(--line-2)',
              borderRadius: 'var(--r-md)',
              boxShadow: '0 8px 24px rgba(0,0,0,0.4)',
              overflow: 'hidden',
            }}
          >
            {suggestions.map((s) => (
              <button
                key={s}
                type="button"
                style={{
                  display: 'block',
                  width: '100%',
                  padding: '8px 14px',
                  textAlign: 'left',
                  background: 'none',
                  border: 'none',
                  cursor: 'pointer',
                  fontFamily: 'var(--font-mono)',
                  fontSize: 12,
                  color: 'var(--fg-2)',
                  borderBottom: '1px solid var(--line-1)',
                }}
                onMouseEnter={(e) => {
                  ;(e.currentTarget as HTMLButtonElement).style.background = 'var(--accent-ghost)'
                  ;(e.currentTarget as HTMLButtonElement).style.color = 'var(--fg-1)'
                }}
                onMouseLeave={(e) => {
                  ;(e.currentTarget as HTMLButtonElement).style.background = 'none'
                  ;(e.currentTarget as HTMLButtonElement).style.color = 'var(--fg-2)'
                }}
                onMouseDown={(e) => {
                  e.preventDefault()
                  onTagChange(s)
                  setShowSuggestions(false)
                }}
              >
                {s}
              </button>
            ))}
            {tag && (
              <button
                type="button"
                style={{
                  display: 'block',
                  width: '100%',
                  padding: '7px 14px',
                  textAlign: 'left',
                  background: 'none',
                  border: 'none',
                  cursor: 'pointer',
                  fontFamily: 'var(--font-mono)',
                  fontSize: 11,
                  color: 'var(--fg-4)',
                }}
                onMouseDown={(e) => {
                  e.preventDefault()
                  onTagChange('')
                  setShowSuggestions(false)
                }}
              >
                {t('blocks.clearFilter')}
              </button>
            )}
          </div>
        )}
      </div>

      <LxSelect
        value={languageId !== undefined ? String(languageId) : 'all'}
        onValueChange={(v) => onLanguageChange(v === 'all' ? undefined : Number(v))}
        triggerStyle={{ width: '100%', maxWidth: 160 }}
        options={[
          { value: 'all', label: t('common.allLanguages') },
          ...Object.entries(LANGUAGES).map(([id, lang]) => ({ value: id, label: lang.name })),
        ]}
      />
    </div>
  )
}
