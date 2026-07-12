import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import {
  Button,
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  LxSelect,
  Spinner,
} from '@/shared/ui'
import { useBlocks } from '@/entities/block'

interface MoveWordsDialogProps {
  open: boolean
  onClose: () => void
  /** Words can only move between blocks of the same language (backend enforces this too). */
  languageId: number
  currentBlockId: string
  selectedCount: number
  onConfirm: (targetBlockId: string) => void
  isPending: boolean
}

export function MoveWordsDialog({
  open,
  onClose,
  languageId,
  currentBlockId,
  selectedCount,
  onConfirm,
  isPending,
}: MoveWordsDialogProps) {
  const { t } = useTranslation()
  const [targetBlockId, setTargetBlockId] = useState('')

  const { data, isLoading } = useBlocks({ languageId, page: 1, pageSize: 50 })
  const targets = (data?.items ?? []).filter((b) => b.id !== currentBlockId)

  const handleClose = () => {
    setTargetBlockId('')
    onClose()
  }

  return (
    <Dialog open={open} onOpenChange={(v) => !v && handleClose()}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>{t('blockDetail.moveTitle', { count: selectedCount })}</DialogTitle>
        </DialogHeader>

        {isLoading ? (
          <div className="flex justify-center py-6">
            <Spinner />
          </div>
        ) : targets.length === 0 ? (
          <p className="py-2 text-sm text-muted-foreground">{t('blockDetail.noTargetBlocks')}</p>
        ) : (
          <LxSelect
            value={targetBlockId}
            onValueChange={setTargetBlockId}
            placeholder={t('blockDetail.moveSelectBlock')}
            options={targets.map((b) => ({
              value: b.id,
              label: `${b.title} (${t('blocks.wordCount', { count: b.wordCount })})`,
            }))}
          />
        )}

        <DialogFooter>
          <Button variant="outline" onClick={handleClose}>
            {t('common.cancel')}
          </Button>
          <Button
            onClick={() => targetBlockId && onConfirm(targetBlockId)}
            disabled={!targetBlockId || isPending}
          >
            {isPending ? t('blockDetail.moving') : t('blockDetail.moveSelected')}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
