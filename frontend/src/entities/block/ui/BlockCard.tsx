import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { ROUTES, LANGUAGES } from '@/shared/config'
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
  const [hovered, setHovered] = useState(false)
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
        onMouseEnter={() => setHovered(true)}
        onMouseLeave={() => setHovered(false)}
        style={{
          padding: 20,
          background: 'var(--bg-2)',
          border: `1px solid ${hovered ? 'var(--accent-line)' : 'var(--line-2)'}`,
          borderRadius: 'var(--r-lg)',
          cursor: 'pointer',
          transition: 'border-color 0.15s, box-shadow 0.15s',
          boxShadow: hovered ? 'var(--glow-accent)' : 'none',
          outline: 'none',
        }}
      >
        <div
          style={{
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'start',
            gap: 10,
            marginBottom: 18,
          }}
        >
          <div className="ds-h4" style={{ color: 'var(--fg-1)', fontSize: 16 }}>
            {block.title}
          </div>
          <span
            style={{
              fontFamily: 'var(--font-mono)',
              fontSize: 11,
              padding: '3px 8px',
              borderRadius: 'var(--r-sm)',
              background: 'var(--bg-1)',
              border: '1px solid var(--line-2)',
              color: 'var(--fg-2)',
              flexShrink: 0,
            }}
          >
            {langCode.toUpperCase()}
          </span>
        </div>

        {block.tags.length > 0 && (
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: 6, marginBottom: 14 }}>
            {block.tags.map((tag) => (
              <span key={tag} className="lx-tag">
                {tag}
              </span>
            ))}
          </div>
        )}

        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
          <div className="ds-code" style={{ color: 'var(--fg-3)' }}>
            {block.wordCount} words
          </div>
          <div
            style={{
              display: 'flex',
              gap: 8,
              opacity: hovered ? 1 : 0,
              transition: 'opacity 0.15s',
            }}
          >
            <button
              onClick={handleEdit}
              className="lx-btn-secondary"
              style={{ padding: '5px 12px', fontSize: 12 }}
            >
              Edit
            </button>
            <button
              onClick={handleDelete}
              disabled={deleteBlock.isPending}
              style={{
                padding: '5px 12px',
                fontSize: 12,
                fontFamily: 'var(--font-body)',
                fontWeight: 600,
                borderRadius: 'var(--r-md)',
                cursor: 'pointer',
                border: '1px solid rgba(255,92,108,0.3)',
                background: 'transparent',
                color: 'var(--danger)',
                transition: 'all 0.12s',
              }}
            >
              Delete
            </button>
          </div>
          {!hovered && (
            <span
              style={{ color: 'var(--accent-color)', fontFamily: 'var(--font-mono)', fontSize: 13 }}
            >
              →
            </span>
          )}
        </div>
      </div>

      {editing && <EditBlockModal block={block} open={editing} onClose={() => setEditing(false)} />}
    </>
  )
}
