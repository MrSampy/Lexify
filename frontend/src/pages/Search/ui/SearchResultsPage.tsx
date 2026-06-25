import { Link, useSearchParams } from 'react-router-dom'
import { Badge, Spinner } from '@/shared/ui'
import { ROUTES } from '@/shared/config'
import { useSearchWords } from '@/entities/word/api/searchApi'

export function SearchResultsPage() {
  const [searchParams] = useSearchParams()
  const q = searchParams.get('q') ?? ''
  const { data, isLoading, isError } = useSearchWords(q)

  return (
    <div className="mx-auto max-w-3xl px-4 py-8">
      <h1 className="mb-1 text-xl font-bold">Search results</h1>
      {q && <p className="mb-6 text-sm text-muted-foreground">for "{q}"</p>}

      {q.trim().length < 2 && (
        <p className="text-muted-foreground">Type at least 2 characters to search.</p>
      )}

      {q.trim().length >= 2 && isLoading && <Spinner />}
      {isError && <p className="text-destructive">Search failed. Please try again.</p>}

      {!isLoading && !isError && data?.length === 0 && q.trim().length >= 2 && (
        <p className="text-muted-foreground">No words found matching "{q}".</p>
      )}

      <ul className="space-y-2">
        {data?.map((result) => (
          <li key={result.wordId}>
            <Link
              to={ROUTES.BLOCK_DETAIL(result.blockId)}
              className="flex items-center justify-between rounded-lg border bg-card p-3 transition-shadow hover:shadow-sm"
            >
              <div>
                <span className="font-medium">{result.term}</span>
                <span className="mx-2 text-muted-foreground">→</span>
                <span>{result.translation}</span>
              </div>
              <div className="flex items-center gap-2">
                <Badge variant="outline" className="text-xs capitalize">
                  {result.wordType}
                </Badge>
                <span className="text-xs text-muted-foreground">{result.blockTitle}</span>
              </div>
            </Link>
          </li>
        ))}
      </ul>
    </div>
  )
}
