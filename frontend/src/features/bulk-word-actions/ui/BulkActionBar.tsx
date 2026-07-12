import { useState } from 'react'
import { toast } from 'sonner'
import { useTranslation } from 'react-i18next'
import { useConfirm } from '@/shared/ui'
import { useBulkDeleteWordsMutation, useBulkMoveWordsMutation } from '@/entities/word'
import { MoveWordsDialog } from './MoveWordsDialog'

interface BulkActionBarProps {
  blockId: string
  languageId: number
  selectedIds: string[]
  onDone: () => void
}

/** Action bar shown above the word table while at least one word is selected. */
export function BulkActionBar({ blockId, languageId, selectedIds, onDone }: BulkActionBarProps) {
  const { t } = useTranslation()
  const [moveOpen, setMoveOpen] = useState(false)
  const bulkDelete = useBulkDeleteWordsMutation(blockId)
  const bulkMove = useBulkMoveWordsMutation(blockId)
  const { confirm, confirmDialog } = useConfirm()

  if (selectedIds.length === 0) return null

  const handleDelete = async () => {
    const ok = await confirm({
      title: t('blockDetail.deleteSelectedConfirm', { count: selectedIds.length }),
    })
    if (!ok) return
    bulkDelete.mutate(selectedIds, {
      onSuccess: (count) => {
        toast.success(t('blockDetail.deleteSuccess', { count }))
        onDone()
      },
      onError: () => toast.error(t('blockDetail.deleteFailed')),
    })
  }

  const handleMove = (targetBlockId: string) => {
    bulkMove.mutate(
      { targetBlockId, wordIds: selectedIds },
      {
        onSuccess: (count) => {
          toast.success(t('blockDetail.moveSuccess', { count }))
          setMoveOpen(false)
          onDone()
        },
        onError: () => toast.error(t('blockDetail.moveFailed')),
      },
    )
  }

  return (
    <div
      style={{
        display: 'flex',
        alignItems: 'center',
        flexWrap: 'wrap',
        gap: 10,
        padding: '10px 14px',
        marginBottom: 10,
        background: 'var(--accent-ghost)',
        border: '1px solid var(--accent-line)',
        borderRadius: 'var(--r-md)',
      }}
    >
      <span className="ds-sm" style={{ color: 'var(--fg-2)', fontWeight: 600, flex: 1 }}>
        {t('blockDetail.selectedCount', { count: selectedIds.length })}
      </span>
      <button
        className="lx-btn-secondary"
        style={{ padding: '6px 14px', fontSize: 13 }}
        onClick={() => setMoveOpen(true)}
        disabled={bulkMove.isPending || bulkDelete.isPending}
      >
        {t('blockDetail.moveSelected')}
      </button>
      <button
        className="lx-btn-secondary"
        style={{ padding: '6px 14px', fontSize: 13, color: 'var(--danger)' }}
        onClick={() => void handleDelete()}
        disabled={bulkMove.isPending || bulkDelete.isPending}
      >
        {t('blockDetail.deleteSelected')}
      </button>
      <button
        className="cursor-pointer border-none bg-transparent text-[13px] text-[var(--fg-4)] hover:text-[var(--fg-2)] [font-family:var(--font-body)]"
        onClick={onDone}
      >
        {t('blockDetail.clearSelection')}
      </button>

      <MoveWordsDialog
        open={moveOpen}
        onClose={() => setMoveOpen(false)}
        languageId={languageId}
        currentBlockId={blockId}
        selectedCount={selectedIds.length}
        onConfirm={handleMove}
        isPending={bulkMove.isPending}
      />
      {confirmDialog}
    </div>
  )
}
