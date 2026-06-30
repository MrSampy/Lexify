import { useBlocks } from '@/entities/block'
import { useGenerateTestStore } from '../model/store'

export function BlockSelector() {
  const { data, isLoading } = useBlocks({ page: 1, pageSize: 100 })
  const selectedBlockIds = useGenerateTestStore((s) => s.selectedBlockIds)
  const toggleBlock = useGenerateTestStore((s) => s.toggleBlock)

  if (isLoading) {
    return <p style={{ color: 'var(--fg-4)', fontSize: 12, fontWeight: 600 }}>Loading blocks…</p>
  }

  if (!data || data.items.length === 0) {
    return (
      <p style={{ color: 'var(--fg-4)', fontSize: 12, fontWeight: 600 }}>
        No blocks found. Create a block first.
      </p>
    )
  }

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
      {data.items.map((block) => {
        const isSelected = selectedBlockIds.includes(block.id)
        return (
          <label
            key={block.id}
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: 12,
              padding: '10px 14px',
              background: isSelected ? 'var(--accent-ghost)' : 'var(--bg-3)',
              border: `1px solid ${isSelected ? 'var(--accent-line)' : 'var(--line-2)'}`,
              borderRadius: 'var(--r-md)',
              cursor: 'pointer',
              transition: 'border-color 0.12s, background 0.12s',
            }}
          >
            <input
              type="checkbox"
              checked={isSelected}
              onChange={() => toggleBlock(block.id)}
              style={{ accentColor: 'var(--accent-color)', width: 14, height: 14, flexShrink: 0 }}
            />
            <div style={{ minWidth: 0 }}>
              <p
                style={{
                  margin: 0,
                  fontSize: 13,
                  fontWeight: 500,
                  color: 'var(--fg-1)',
                  overflow: 'hidden',
                  textOverflow: 'ellipsis',
                  whiteSpace: 'nowrap',
                }}
              >
                {block.title}
              </p>
              <p style={{ margin: 0, fontSize: 10, color: 'var(--fg-4)', fontWeight: 600 }}>
                {block.wordCount} words
              </p>
            </div>
          </label>
        )
      })}
    </div>
  )
}
