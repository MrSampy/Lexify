import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { ROUTES, LANGUAGES } from '@/shared/config'
import { Badge, Button, LanguageBadge } from '@/shared/ui'
import { useDeleteBlockMutation } from '../api/blockApi'
import type { WordBlock } from '../model/types'
import { EditBlockModal } from './EditBlockModal'

interface BlockCardProps {
  block: WordBlock
}

export function BlockCard({ block }: BlockCardProps) {
  const navigate = useNavigate()
  const deleteBlock = useDeleteBlockMutation()
  const [editing, setEditing] = useState(false)
  const langCode = LANGUAGES[block.languageId]?.code ?? String(block.languageId)

  const handleDelete = (e: React.MouseEvent) => {
    e.stopPropagation()
    if (!confirm(`Delete "${block.title}"?`)) return
    deleteBlock.mutate(block.id)
  }

  const handleEdit = (e: React.MouseEvent) => {
    e.stopPropagation()
    setEditing(true)
  }

  return (
    <>
      <div
        role="button"
        tabIndex={0}
        onClick={() => navigate(ROUTES.BLOCK_DETAIL(block.id))}
        onKeyDown={(e) => e.key === 'Enter' && navigate(ROUTES.BLOCK_DETAIL(block.id))}
        className="group flex cursor-pointer flex-col gap-3 rounded-xl border bg-card p-4 shadow-sm transition-shadow hover:shadow-md focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
      >
        <div className="flex items-start justify-between gap-2">
          <h3 className="font-semibold leading-tight">{block.title}</h3>
          <LanguageBadge code={langCode} />
        </div>

        {block.description && (
          <p className="line-clamp-2 text-sm text-muted-foreground">{block.description}</p>
        )}

        {block.tags.length > 0 && (
          <div className="flex flex-wrap gap-1">
            {block.tags.map((tag) => (
              <Badge key={tag} variant="secondary" className="text-xs">
                {tag}
              </Badge>
            ))}
          </div>
        )}

        <div className="flex items-center justify-between">
          <span className="text-xs text-muted-foreground">
            {block.wordCount} {block.wordCount === 1 ? 'word' : 'words'}
          </span>
          <div className="flex gap-1 opacity-0 transition-opacity group-hover:opacity-100">
            <Button size="sm" variant="ghost" onClick={handleEdit} className="h-7 px-2 text-xs">
              Edit
            </Button>
            <Button
              size="sm"
              variant="ghost"
              onClick={handleDelete}
              disabled={deleteBlock.isPending}
              className="h-7 px-2 text-xs text-destructive hover:text-destructive"
            >
              Delete
            </Button>
          </div>
        </div>
      </div>

      {editing && <EditBlockModal block={block} open={editing} onClose={() => setEditing(false)} />}
    </>
  )
}
