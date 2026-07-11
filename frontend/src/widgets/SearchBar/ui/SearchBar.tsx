import { useCallback, useRef, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { Search } from 'lucide-react'
import { debounce } from '@/shared/lib/debounce'
import { ROUTES } from '@/shared/config'

export function SearchBar() {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
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
    // Fluid up to 200px so the bar shrinks gracefully in the mobile top bar.
    <div style={{ position: 'relative', flex: '1 1 auto', maxWidth: 200, minWidth: 0 }}>
      <Search
        style={{
          position: 'absolute',
          left: 10,
          top: '50%',
          transform: 'translateY(-50%)',
          width: 13,
          height: 13,
          color: 'var(--fg-4)',
          pointerEvents: 'none',
        }}
      />
      <input
        className="lx-input"
        placeholder="search words…"
        value={value}
        onChange={handleChange}
        onKeyDown={handleKeyDown}
        style={{ height: 34, width: '100%', paddingLeft: 30, fontSize: 13 }}
      />
    </div>
  )
}
