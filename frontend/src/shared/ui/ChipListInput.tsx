import { useState } from 'react'
import { X } from 'lucide-react'
import { Badge, Input } from '@/shared/ui'

interface ChipListInputProps {
  /** Current list of values (controlled). */
  value: string[]
  /** Called with the next list whenever a chip is added or removed. */
  onChange: (next: string[]) => void
  placeholder?: string
  disabled?: boolean
  className?: string
}

/**
 * Controlled "list of strings" editor: renders each value as a removable chip and an input that
 * appends a new value on Enter. Case-insensitive de-dupe; blanks are ignored. Decoupled from any
 * data source — the parent owns the list (used for word synonyms, and reusable elsewhere).
 */
export function ChipListInput({
  value,
  onChange,
  placeholder,
  disabled,
  className,
}: ChipListInputProps) {
  const [inputValue, setInputValue] = useState('')

  const add = (raw: string) => {
    const trimmed = raw.trim()
    if (!trimmed) return
    if (value.some((v) => v.toLowerCase() === trimmed.toLowerCase())) {
      setInputValue('')
      return
    }
    onChange([...value, trimmed])
    setInputValue('')
  }

  const remove = (item: string) => {
    onChange(value.filter((v) => v !== item))
  }

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter') {
      e.preventDefault()
      add(inputValue)
    } else if (e.key === 'Backspace' && inputValue.length === 0 && value.length > 0) {
      remove(value[value.length - 1])
    }
  }

  return (
    <div className={`space-y-2 ${className ?? ''}`}>
      {value.length > 0 && (
        <div className="flex flex-wrap gap-1">
          {value.map((item) => (
            <Badge key={item} variant="secondary" className="gap-1 pr-1 text-xs">
              {item}
              <button
                type="button"
                onClick={() => remove(item)}
                disabled={disabled}
                className="ml-0.5 rounded-sm hover:bg-muted-foreground/20 focus-visible:outline-none disabled:opacity-50"
                aria-label={`Remove ${item}`}
              >
                <X className="h-3 w-3" />
              </button>
            </Badge>
          ))}
        </div>
      )}

      <Input
        value={inputValue}
        onChange={(e) => setInputValue(e.target.value)}
        onKeyDown={handleKeyDown}
        onBlur={() => add(inputValue)}
        placeholder={placeholder}
        className="h-8 text-sm"
        disabled={disabled}
      />
    </div>
  )
}
