import { useCallback, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Button,
} from '@/shared/ui'

interface ConfirmOptions {
  title: string
  description?: string
  confirmLabel?: string
  cancelLabel?: string
  /** Styles the confirm button red for destructive actions (default true) */
  destructive?: boolean
}

/**
 * Promise-based replacement for window.confirm():
 *
 *   const { confirm, confirmDialog } = useConfirm()
 *   ...
 *   if (!(await confirm({ title: 'Delete block?' }))) return
 *   ...
 *   return <>{confirmDialog}...</>
 */
export function useConfirm() {
  const { t } = useTranslation()
  const [options, setOptions] = useState<ConfirmOptions | null>(null)
  const resolverRef = useRef<((confirmed: boolean) => void) | null>(null)

  const confirm = useCallback((opts: ConfirmOptions) => {
    setOptions(opts)
    return new Promise<boolean>((resolve) => {
      resolverRef.current = resolve
    })
  }, [])

  const close = useCallback((confirmed: boolean) => {
    resolverRef.current?.(confirmed)
    resolverRef.current = null
    setOptions(null)
  }, [])

  const confirmDialog = (
    <Dialog open={options !== null} onOpenChange={(open) => !open && close(false)}>
      {options && (
        <DialogContent showCloseButton={false} className="max-w-sm">
          <DialogHeader>
            <DialogTitle>{options.title}</DialogTitle>
            {options.description && <DialogDescription>{options.description}</DialogDescription>}
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => close(false)}>
              {options.cancelLabel ?? t('common.cancel')}
            </Button>
            <Button
              variant={options.destructive === false ? 'default' : 'destructive'}
              onClick={() => close(true)}
            >
              {options.confirmLabel ?? t('common.delete')}
            </Button>
          </DialogFooter>
        </DialogContent>
      )}
    </Dialog>
  )

  return { confirm, confirmDialog }
}
