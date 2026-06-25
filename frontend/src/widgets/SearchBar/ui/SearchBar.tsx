import { useCallback, useRef, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { Search } from 'lucide-react'
import { Input } from '@/shared/ui'
import { debounce } from '@/shared/lib/debounce'
import { ROUTES } from '@/shared/config'

export function SearchBar() {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  // Initialize from URL; use key on the page level to re-mount on navigation if needed
  const [value, setValue] = useState(() => searchParams.get('q') ?? '')

  const debouncedNavigate = useRef(
    debounce((q: string) => {
      if (q.trim().length >= 2) {
        navigate(`${ROUTES.SEARCH}?q=${encodeURIComponent(q.trim())}`)
      }
    }, 300),
  ).current

  const handleChange = useCallback(
    (e: React.ChangeEvent<HTMLInputElement>) => {
      setValue(e.target.value)
      debouncedNavigate(e.target.value)
    },
    [debouncedNavigate],
  )

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter' && value.trim().length >= 2) {
      navigate(`${ROUTES.SEARCH}?q=${encodeURIComponent(value.trim())}`)
    }
  }

  return (
    <div className="relative">
      <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
      <Input
        placeholder="Search words..."
        value={value}
        onChange={handleChange}
        onKeyDown={handleKeyDown}
        className="h-9 w-64 pl-8"
      />
    </div>
  )
}
