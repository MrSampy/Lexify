import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { ROUTES, LANGUAGES } from '@/shared/config'
import { Button } from '@/shared/ui'
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
      <button
        type="button"
        onClick={() => navigate(ROUTES.BLOCK_DETAIL(block.id))}
        className="group w-full cursor-pointer rounded-[var(--r-lg)] border border-[var(--line-2)] bg-[var(--bg-2)] p-5 text-left outline-none transition-[border-color,box-shadow] duration-150 hover:border-[var(--accent-line)] hover:shadow-[var(--glow-accent)]"
      >
        <div className="mb-[18px] flex items-start justify-between gap-2.5">
          <div className="ds-h4 text-[var(--fg-1)]">{block.title}</div>
          <span className="shrink-0 rounded-[var(--r-pill)] border border-[var(--accent-line)] bg-[var(--accent-ghost)] px-2.5 py-[3px] text-[11px] font-bold text-[var(--accent-dim)] [font-family:var(--font-body)]">
            {langCode.toUpperCase()}
          </span>
        </div>

        {block.tags.length > 0 && (
          <div className="mb-3.5 flex flex-wrap gap-1.5">
            {block.tags.map((tag) => (
              <span key={tag} className="lx-tag">
                {tag}
              </span>
            ))}
          </div>
        )}

        <div className="flex items-center justify-between">
          <div className="ds-sm text-[var(--fg-3)]">{block.wordCount} words</div>
          <div className="flex gap-2 opacity-0 transition-opacity duration-150 group-hover:opacity-100">
            <button
              onClick={handleEdit}
              className="lx-btn-secondary"
              style={{ padding: '5px 12px', fontSize: 12 }}
            >
              Edit
            </button>
            <Button
              variant="destructive"
              size="sm"
              onClick={handleDelete}
              disabled={deleteBlock.isPending}
            >
              Delete
            </Button>
          </div>
          <span className="text-[13px] font-bold text-[var(--accent-color)] [font-family:var(--font-body)] group-hover:hidden">
            →
          </span>
        </div>
      </button>

      {editing && <EditBlockModal block={block} open={editing} onClose={() => setEditing(false)} />}
    </>
  )
}
