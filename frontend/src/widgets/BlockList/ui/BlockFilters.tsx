import { useRef, useState } from 'react'
import { LANGUAGES } from '@/shared/config'
import { Input, Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/shared/ui'
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
  const [showSuggestions, setShowSuggestions] = useState(false)
  const inputRef = useRef<HTMLInputElement>(null)
  const { data: userTags = [] } = useUserTags()

  const suggestions = tag.trim()
    ? userTags.filter((t) => t.includes(tag.trim().toLowerCase())).slice(0, 6)
    : userTags.slice(0, 6)

  return (
    <div className="flex flex-wrap gap-2">
      <div className="relative">
        <Input
          ref={inputRef}
          placeholder="Filter by tag..."
          value={tag}
          onChange={(e) => onTagChange(e.target.value)}
          onFocus={() => setShowSuggestions(true)}
          onBlur={() => setTimeout(() => setShowSuggestions(false), 150)}
          className="h-9 w-48"
        />
        {showSuggestions && suggestions.length > 0 && (
          <div className="absolute left-0 top-full z-10 mt-1 w-full rounded-md border bg-popover shadow-md">
            {suggestions.map((s) => (
              <button
                key={s}
                type="button"
                className="w-full px-3 py-1.5 text-left text-sm hover:bg-accent"
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
                className="w-full border-t px-3 py-1.5 text-left text-xs text-muted-foreground hover:bg-accent"
                onMouseDown={(e) => {
                  e.preventDefault()
                  onTagChange('')
                  setShowSuggestions(false)
                }}
              >
                Clear filter
              </button>
            )}
          </div>
        )}
      </div>

      <Select
        value={languageId !== undefined ? String(languageId) : 'all'}
        onValueChange={(v) => onLanguageChange(v === 'all' ? undefined : Number(v))}
      >
        <SelectTrigger className="h-9 w-40">
          <SelectValue placeholder="All languages" />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="all">All languages</SelectItem>
          {Object.entries(LANGUAGES).map(([id, lang]) => (
            <SelectItem key={id} value={id}>
              {lang.name}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
    </div>
  )
}
