import { useCallback, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useSearchParams } from 'react-router-dom'
import { LxSelect, Mascot, Spinner } from '@/shared/ui'
import { LANGUAGES, ROUTES } from '@/shared/config'
import { debounce } from '@/shared/lib/debounce'
import { useSearchWords } from '@/entities/word/api/searchApi'
import { WordTypeBadge } from '@/entities/word'

export function SearchResultsPage() {
  const { t } = useTranslation()
  const [searchParams, setSearchParams] = useSearchParams()
  const q = searchParams.get('q') ?? ''
  const langParam = searchParams.get('lang')
  const lang = langParam ? Number(langParam) : undefined
  const { data, isLoading, isError } = useSearchWords(q, lang)

  const [inputValue, setInputValue] = useState(q)

  const debouncedSetQuery = useRef(
    debounce((value: string) => {
      setSearchParams((prev) => {
        const next = new URLSearchParams(prev)
        if (value.trim()) next.set('q', value.trim())
        else next.delete('q')
        return next
      })
    }, 300),
  ).current

  const handleLangChange = (v: string) => {
    setSearchParams((prev) => {
      const next = new URLSearchParams(prev)
      if (v === 'all') next.delete('lang')
      else next.set('lang', v)
      return next
    })
  }

  const handleChange = useCallback(
    (e: React.ChangeEvent<HTMLInputElement>) => {
      setInputValue(e.target.value)
      debouncedSetQuery(e.target.value)
    },
    [debouncedSetQuery],
  )

  // Group results by block
  const grouped = (data ?? []).reduce<
    Record<string, { blockTitle: string; blockId: string; items: typeof data }>
  >((acc, r) => {
    if (!r) return acc
    if (!acc[r.blockId]) {
      acc[r.blockId] = { blockTitle: r.blockTitle, blockId: r.blockId, items: [] }
    }
    acc[r.blockId].items!.push(r)
    return acc
  }, {})

  return (
    <div style={{ maxWidth: 760, margin: '0 auto' }}>
      {/* Search input + language filter */}
      <div style={{ display: 'flex', flexWrap: 'wrap', gap: 10, marginBottom: 8 }}>
        <div
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: 12,
            flex: 1,
            minWidth: 220,
            padding: '14px 18px',
            background: 'var(--bg-2)',
            border: `1px solid ${inputValue ? 'var(--accent-line)' : 'var(--line-2)'}`,
            borderRadius: 'var(--r-md)',
            boxShadow: inputValue ? '0 0 0 3px var(--accent-ghost)' : 'none',
          }}
        >
          <span style={{ color: 'var(--accent-color)', fontSize: 16 }}>🔍</span>
          <input
            autoFocus
            value={inputValue}
            onChange={handleChange}
            placeholder={t('search.placeholder')}
            style={{
              flex: 1,
              border: 'none',
              outline: 'none',
              background: 'transparent',
              color: 'var(--fg-1)',
              fontSize: 15,
              fontFamily: 'var(--font-body)',
            }}
          />
          {isLoading && <Spinner />}
        </div>
        <LxSelect
          value={lang !== undefined ? String(lang) : 'all'}
          onValueChange={handleLangChange}
          triggerStyle={{ width: 160, alignSelf: 'center' }}
          options={[
            { value: 'all', label: t('common.allLanguages') },
            ...Object.entries(LANGUAGES).map(([id, l]) => ({ value: id, label: l.name })),
          ]}
        />
      </div>

      {q && data && (
        <div className="ds-sm" style={{ color: 'var(--fg-4)', marginBottom: 24, fontWeight: 600 }}>
          {t('search.matches', { count: data.length })}{' '}
          {t('search.inBlocks', { count: Object.keys(grouped).length })}
        </div>
      )}

      {q.trim().length < 2 && (
        <p className="ds-sm" style={{ color: 'var(--fg-3)' }}>
          {t('search.minChars')}
        </p>
      )}

      {isError && (
        <p style={{ color: 'var(--danger)', fontFamily: 'var(--font-body)', fontSize: 13 }}>
          {t('search.failed')}
        </p>
      )}

      {!isLoading && !isError && data?.length === 0 && q.trim().length >= 2 && (
        <div
          style={{
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            gap: 12,
            padding: '48px 0',
            textAlign: 'center',
          }}
        >
          <Mascot pose="searching" size={120} float />
          <p className="ds-sm" style={{ margin: 0, color: 'var(--fg-3)' }}>
            {t('search.nothing', { query: q })}
          </p>
        </div>
      )}

      {/* Grouped results */}
      {Object.values(grouped).map((group) => (
        <div key={group.blockId} style={{ marginBottom: 22 }}>
          <div
            style={{
              display: 'flex',
              alignItems: 'baseline',
              gap: 12,
              marginBottom: 10,
            }}
          >
            <span
              style={{
                width: 6,
                height: 6,
                borderRadius: 999,
                background: 'var(--accent-color)',
                flexShrink: 0,
                alignSelf: 'center',
              }}
            />
            <h2 className="ds-h4" style={{ margin: 0, fontSize: 13, color: 'var(--fg-2)' }}>
              {group.blockTitle}
            </h2>
          </div>
          <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
            {group.items?.map(
              (r) =>
                r && (
                  <Link
                    key={r.wordId}
                    to={ROUTES.BLOCK_DETAIL(r.blockId)}
                    style={{ textDecoration: 'none' }}
                  >
                    <div
                      style={{
                        display: 'flex',
                        alignItems: 'center',
                        gap: 14,
                        padding: '13px 18px',
                        background: 'var(--bg-2)',
                        border: '1px solid var(--line-2)',
                        borderRadius: 'var(--r-md)',
                        transition: 'border-color 0.12s',
                      }}
                      onMouseEnter={(e) => {
                        ;(e.currentTarget as HTMLDivElement).style.borderColor =
                          'var(--accent-line)'
                      }}
                      onMouseLeave={(e) => {
                        ;(e.currentTarget as HTMLDivElement).style.borderColor = 'var(--line-2)'
                      }}
                    >
                      <span style={{ color: 'var(--fg-1)', fontWeight: 500, fontSize: 15 }}>
                        {r.term}
                      </span>
                      <span style={{ color: 'var(--fg-4)' }}>→</span>
                      <span style={{ flex: 1, color: 'var(--fg-2)', fontSize: 14 }}>
                        {r.translation}
                      </span>
                      <WordTypeBadge type={r.wordType} />
                    </div>
                  </Link>
                ),
            )}
          </div>
        </div>
      ))}
    </div>
  )
}
