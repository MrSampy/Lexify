import { useCallback, useRef, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { Spinner } from '@/shared/ui'
import { ROUTES } from '@/shared/config'
import { debounce } from '@/shared/lib/debounce'
import { useSearchWords } from '@/entities/word/api/searchApi'
import { WordTypeBadge } from '@/entities/word'

export function SearchResultsPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const q = searchParams.get('q') ?? ''
  const { data, isLoading, isError } = useSearchWords(q)

  const [inputValue, setInputValue] = useState(q)

  const debouncedSetQuery = useRef(
    debounce((value: string) => {
      setSearchParams(value.trim() ? { q: value.trim() } : {})
    }, 300),
  ).current

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
      {/* Search input */}
      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          gap: 12,
          padding: '14px 18px',
          background: 'var(--bg-2)',
          border: `1px solid ${inputValue ? 'var(--accent-line)' : 'var(--line-2)'}`,
          borderRadius: 'var(--r-md)',
          marginBottom: 8,
          boxShadow: inputValue ? '0 0 0 3px var(--accent-ghost)' : 'none',
        }}
      >
        <span style={{ color: 'var(--accent-color)', fontSize: 16 }}>🔍</span>
        <input
          autoFocus
          value={inputValue}
          onChange={handleChange}
          placeholder="type to search…"
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

      {q && data && (
        <div className="ds-sm" style={{ color: 'var(--fg-4)', marginBottom: 24, fontWeight: 600 }}>
          {data.length} match{data.length !== 1 ? 'es' : ''} across {Object.keys(grouped).length}{' '}
          block{Object.keys(grouped).length !== 1 ? 's' : ''}
        </div>
      )}

      {q.trim().length < 2 && (
        <p className="ds-sm" style={{ color: 'var(--fg-3)' }}>
          Type at least 2 characters to search.
        </p>
      )}

      {isError && (
        <p style={{ color: 'var(--danger)', fontFamily: 'var(--font-body)', fontSize: 13 }}>
          Search failed. Please try again.
        </p>
      )}

      {!isLoading && !isError && data?.length === 0 && q.trim().length >= 2 && (
        <p className="ds-sm" style={{ color: 'var(--fg-3)' }}>
          Nothing found for "{q}".
        </p>
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
