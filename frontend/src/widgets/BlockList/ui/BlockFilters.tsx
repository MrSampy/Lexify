import { LANGUAGES } from '@/shared/config'
import { Input, Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/shared/ui'

interface BlockFiltersProps {
  languageId: number | undefined
  onLanguageChange: (id: number | undefined) => void
  search: string
  onSearchChange: (v: string) => void
}

export function BlockFilters({
  languageId,
  onLanguageChange,
  search,
  onSearchChange,
}: BlockFiltersProps) {
  return (
    <div className="flex flex-wrap gap-2">
      <Input
        placeholder="Search by title..."
        value={search}
        onChange={(e) => onSearchChange(e.target.value)}
        className="h-9 w-48"
      />
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
