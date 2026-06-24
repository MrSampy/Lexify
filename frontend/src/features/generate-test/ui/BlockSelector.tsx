import { Checkbox } from '@/shared/ui'
import { useBlocks } from '@/entities/block'
import { useGenerateTestStore } from '../model/store'

export function BlockSelector() {
  const { data, isLoading } = useBlocks({ page: 1, pageSize: 100 })
  const selectedBlockIds = useGenerateTestStore((s) => s.selectedBlockIds)
  const toggleBlock = useGenerateTestStore((s) => s.toggleBlock)

  if (isLoading) {
    return <p className="text-sm text-muted-foreground">Loading blocks…</p>
  }

  if (!data || data.items.length === 0) {
    return <p className="text-sm text-muted-foreground">No blocks found. Create a block first.</p>
  }

  return (
    <div className="space-y-2">
      {data.items.map((block) => (
        <label
          key={block.id}
          className="flex cursor-pointer items-center gap-3 rounded-md p-2 hover:bg-muted/50"
        >
          <Checkbox
            checked={selectedBlockIds.includes(block.id)}
            onCheckedChange={() => toggleBlock(block.id)}
          />
          <div className="min-w-0">
            <p className="truncate text-sm font-medium">{block.title}</p>
            <p className="text-xs text-muted-foreground">{block.wordCount} words</p>
          </div>
        </label>
      ))}
    </div>
  )
}
