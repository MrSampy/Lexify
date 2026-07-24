import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import {
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Input,
  Spinner,
} from '@/shared/ui'
import { ROUTES } from '@/shared/config'
import { useBlockShare, useCreateShareMutation, useRevokeShareMutation } from '@/entities/block'

interface ShareBlockModalProps {
  blockId: string
  open: boolean
  onClose: () => void
}

/**
 * Turn a block's share link on or off. The link is read-only for whoever opens it — they can copy the
 * block into their own account, which is a snapshot, not a subscription: later edits here don't reach
 * copies already made.
 */
export function ShareBlockModal({ blockId, open, onClose }: ShareBlockModalProps) {
  const { t } = useTranslation()
  const [copied, setCopied] = useState(false)
  // Only queried while the dialog is open — the block page has no other use for the share state.
  const { data: share, isLoading } = useBlockShare(blockId, open)
  const createShare = useCreateShareMutation(blockId)
  const revokeShare = useRevokeShareMutation(blockId)

  const url = share ? `${window.location.origin}${ROUTES.SHARED_BLOCK(share.token)}` : ''

  const handleCopy = async () => {
    try {
      await navigator.clipboard.writeText(url)
      setCopied(true)
      setTimeout(() => setCopied(false), 2000)
    } catch {
      // Clipboard access can be refused (insecure context, denied permission) — the input is
      // selectable, so say so rather than failing silently.
      toast.error(t('blocks.share.copyFailed'))
    }
  }

  const handleRevoke = async () => {
    await revokeShare.mutateAsync()
    toast.success(t('blocks.share.revoked'))
  }

  return (
    <Dialog open={open} onOpenChange={(v) => !v && onClose()}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>{t('blocks.share.title')}</DialogTitle>
          <DialogDescription>{t('blocks.share.description')}</DialogDescription>
        </DialogHeader>

        {isLoading ? (
          <div className="flex justify-center py-6">
            <Spinner />
          </div>
        ) : share ? (
          <div className="space-y-3">
            <div className="flex gap-2">
              <Input
                readOnly
                value={url}
                onFocus={(e) => e.currentTarget.select()}
                className="text-xs"
                aria-label={t('blocks.share.linkLabel')}
              />
              <Button type="button" variant="outline" onClick={() => void handleCopy()}>
                {copied ? t('blocks.share.copied') : t('blocks.share.copy')}
              </Button>
            </div>
            <p className="text-xs text-[var(--fg-4)]">
              {t('blocks.share.stats', { views: share.viewCount, copies: share.copyCount })}
            </p>
          </div>
        ) : (
          <p className="text-sm text-[var(--fg-3)]">{t('blocks.share.off')}</p>
        )}

        <DialogFooter>
          {share ? (
            <Button
              type="button"
              variant="outline"
              onClick={() => void handleRevoke()}
              disabled={revokeShare.isPending}
            >
              {t('blocks.share.revoke')}
            </Button>
          ) : (
            <Button
              type="button"
              onClick={() => createShare.mutate()}
              disabled={createShare.isPending || isLoading}
            >
              {t('blocks.share.enable')}
            </Button>
          )}
          <Button type="button" variant="outline" onClick={onClose}>
            {t('common.close')}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
