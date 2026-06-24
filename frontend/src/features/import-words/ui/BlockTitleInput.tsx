import { Sparkles } from 'lucide-react'
import { Input } from '@/shared/ui'
import { useImportWordsStore } from '../model/store'

export function BlockTitleInput() {
  const suggestedTitle = useImportWordsStore((s) => s.suggestedTitle)
  const setSuggestedTitle = useImportWordsStore((s) => s.setSuggestedTitle)

  return (
    <div>
      <label className="mb-1 flex items-center gap-1.5 text-sm font-medium">
        <Sparkles className="h-3.5 w-3.5 text-amber-500" />
        Block title
      </label>
      <Input
        value={suggestedTitle}
        onChange={(e) => setSuggestedTitle(e.target.value)}
        placeholder="Give this block a name…"
      />
      <p className="mt-1 text-xs text-muted-foreground">AI-suggested — edit as needed</p>
    </div>
  )
}
