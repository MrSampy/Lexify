import { useRef, useState } from 'react'
import { X } from 'lucide-react'
import { Badge, Input } from '@/shared/ui'
import { useAddTagMutation, useRemoveTagMutation, useUserTags } from '@/entities/block/api/tagApi'

interface TagInputProps {
  blockId: string
  currentTags: string[]
}

export function TagInput({ blockId, currentTags }: TagInputProps) {
  const [inputValue, setInputValue] = useState('')
  const [showSuggestions, setShowSuggestions] = useState(false)
  const inputRef = useRef<HTMLInputElement>(null)

  const { data: userTags = [] } = useUserTags()
  const addTag = useAddTagMutation(blockId)
  const removeTag = useRemoveTagMutation(blockId)

  const normalized = inputValue.trim().toLowerCase()

  const suggestions = userTags
    .filter((t) => !currentTags.includes(t) && t.includes(normalized))
    .slice(0, 5)

  const showCreate =
    normalized.length > 0 && !currentTags.includes(normalized) && !userTags.includes(normalized)

  const handleAdd = (tagName: string) => {
    const name = tagName.trim().toLowerCase()
    if (!name || currentTags.includes(name)) return
    addTag.mutate(name)
    setInputValue('')
    setShowSuggestions(false)
  }

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter') {
      e.preventDefault()
      if (normalized) handleAdd(normalized)
    }
    if (e.key === 'Escape') {
      setShowSuggestions(false)
    }
  }

  return (
    <div className="space-y-2">
      {currentTags.length > 0 && (
        <div className="flex flex-wrap gap-1">
          {currentTags.map((tag) => (
            <Badge key={tag} variant="secondary" className="gap-1 pr-1 text-xs">
              {tag}
              <button
                type="button"
                onClick={() => removeTag.mutate(tag)}
                disabled={removeTag.isPending}
                className="ml-0.5 rounded-sm hover:bg-muted-foreground/20 focus-visible:outline-none"
                aria-label={`Remove tag ${tag}`}
              >
                <X className="h-3 w-3" />
              </button>
            </Badge>
          ))}
        </div>
      )}

      <div className="relative">
        <Input
          ref={inputRef}
          value={inputValue}
          onChange={(e) => {
            setInputValue(e.target.value)
            setShowSuggestions(true)
          }}
          onFocus={() => setShowSuggestions(true)}
          onBlur={() => setTimeout(() => setShowSuggestions(false), 150)}
          onKeyDown={handleKeyDown}
          placeholder="Add tag..."
          className="h-8 text-sm"
          disabled={addTag.isPending}
        />

        {showSuggestions && (suggestions.length > 0 || showCreate) && (
          <div className="absolute left-0 top-full z-10 mt-1 w-full rounded-md border bg-popover shadow-md">
            {suggestions.map((s) => (
              <button
                key={s}
                type="button"
                className="w-full px-3 py-1.5 text-left text-sm hover:bg-accent"
                onMouseDown={(e) => {
                  e.preventDefault()
                  handleAdd(s)
                }}
              >
                {s}
              </button>
            ))}
            {showCreate && (
              <button
                type="button"
                className="w-full px-3 py-1.5 text-left text-sm text-muted-foreground hover:bg-accent"
                onMouseDown={(e) => {
                  e.preventDefault()
                  handleAdd(normalized)
                }}
              >
                Create tag: <span className="font-medium text-foreground">{normalized}</span>
              </button>
            )}
          </div>
        )}
      </div>
    </div>
  )
}
