import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { Download, Star } from 'lucide-react'
import {
  Badge,
  Button,
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  LxSelect,
  Spinner,
  Textarea,
} from '@/shared/ui'
import { formatDate } from '@/shared/lib'
import {
  adminFeedbackApi,
  useAdminFeedbackDetail,
  useUpdateFeedbackStatusMutation,
  type FeedbackStatus,
} from '@/entities/feedback'

const STATUSES: FeedbackStatus[] = ['new', 'in_progress', 'resolved']

interface FeedbackDetailDialogProps {
  feedbackId: string | null
  onClose: () => void
}

function Row({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div style={{ display: 'flex', gap: 10, fontSize: 13 }}>
      <span style={{ color: 'var(--fg-4)', minWidth: 90, flexShrink: 0 }}>{label}</span>
      <span style={{ minWidth: 0, wordBreak: 'break-word' }}>{value}</span>
    </div>
  )
}

export function FeedbackDetailDialog({ feedbackId, onClose }: FeedbackDetailDialogProps) {
  const { t } = useTranslation()
  const { data, isLoading } = useAdminFeedbackDetail(feedbackId)
  const updateStatus = useUpdateFeedbackStatusMutation()

  // Keyed by ticket id so reopening the dialog on a different ticket resets the draft edits.
  const [draft, setDraft] = useState<{ id: string; status: string; note: string } | null>(null)
  const current =
    draft?.id === data?.id
      ? draft
      : data
        ? { id: data.id, status: data.status, note: data.adminNote ?? '' }
        : null

  const download = async (attachmentId: string, fileName: string) => {
    if (!data) return
    try {
      const blob = await adminFeedbackApi.downloadAttachment(data.id, attachmentId)
      const url = URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = url
      link.download = fileName
      link.click()
      URL.revokeObjectURL(url)
    } catch {
      toast.error(t('adminFeedback.saveFailed'))
    }
  }

  const save = async () => {
    if (!data || !current) return
    try {
      await updateStatus.mutateAsync({
        id: data.id,
        status: current.status,
        adminNote: current.note.trim() || null,
      })
      toast.success(t('adminFeedback.saved'))
      onClose()
    } catch {
      toast.error(t('adminFeedback.saveFailed'))
    }
  }

  return (
    <Dialog open={!!feedbackId} onOpenChange={(o) => !o && onClose()}>
      <DialogContent className="max-h-[85vh] max-w-lg overflow-y-auto">
        <DialogHeader>
          <DialogTitle>
            {data ? t('adminFeedback.detailTitle', { code: data.ticketCode }) : '…'}
          </DialogTitle>
        </DialogHeader>

        {isLoading || !data || !current ? (
          <div className="flex justify-center py-8">
            <Spinner size="lg" />
          </div>
        ) : (
          <div style={{ display: 'grid', gap: 14 }}>
            <div style={{ display: 'grid', gap: 6 }}>
              <Row
                label={t('adminFeedback.colType')}
                value={<Badge variant="outline">{data.type}</Badge>}
              />
              <Row label={t('adminFeedback.colCategory')} value={data.category ?? '—'} />
              {data.rating != null && (
                <Row
                  label={t('adminFeedback.colRating')}
                  value={
                    <span style={{ display: 'inline-flex', alignItems: 'center', gap: 4 }}>
                      {data.rating}
                      <Star style={{ width: 14, height: 14 }} fill="currentColor" />
                    </span>
                  }
                />
              )}
              <Row label={t('adminFeedback.colDate')} value={formatDate(data.createdAt)} />
              <Row label={t('adminFeedback.from')} value={data.userEmail ?? '—'} />
              <Row label={t('adminFeedback.replyTo')} value={data.contactEmail ?? '—'} />
            </div>

            <div>
              <h3 style={{ fontSize: 15, fontWeight: 700, margin: '0 0 6px' }}>{data.subject}</h3>
              <p
                style={{
                  fontSize: 14,
                  margin: 0,
                  whiteSpace: 'pre-wrap',
                  wordBreak: 'break-word',
                  color: 'var(--fg-2)',
                }}
              >
                {data.message}
              </p>
            </div>

            {data.attachments.length > 0 && (
              <div>
                <p className="ds-sm" style={{ fontWeight: 700, margin: '0 0 6px' }}>
                  {t('adminFeedback.attachments')}
                </p>
                <ul style={{ listStyle: 'none', padding: 0, margin: 0, display: 'grid', gap: 6 }}>
                  {data.attachments.map((a) => (
                    <li key={a.id}>
                      <button
                        type="button"
                        onClick={() => download(a.id, a.fileName)}
                        style={{
                          display: 'inline-flex',
                          alignItems: 'center',
                          gap: 8,
                          padding: '6px 10px',
                          border: '1px solid var(--line-2)',
                          borderRadius: 'var(--r-sm)',
                          background: 'var(--bg-3)',
                          color: 'var(--fg-2)',
                          fontSize: 13,
                          cursor: 'pointer',
                          maxWidth: '100%',
                        }}
                      >
                        <Download style={{ width: 14, height: 14, flexShrink: 0 }} />
                        <span style={{ overflow: 'hidden', textOverflow: 'ellipsis' }}>
                          {a.fileName}
                        </span>
                        <span style={{ color: 'var(--fg-4)', flexShrink: 0 }}>
                          {Math.round(a.sizeBytes / 1024)} KB
                        </span>
                      </button>
                    </li>
                  ))}
                </ul>
              </div>
            )}

            <div>
              <label
                className="ds-sm"
                style={{ fontWeight: 700, display: 'block', marginBottom: 6 }}
              >
                {t('adminFeedback.statusLabel')}
              </label>
              <LxSelect
                value={current.status}
                onValueChange={(v) => setDraft({ ...current, status: v })}
                triggerStyle={{ width: '100%', maxWidth: 220 }}
                options={STATUSES.map((s) => ({
                  value: s,
                  label: t(`adminFeedback.status.${s}`),
                }))}
              />
            </div>

            <div>
              <label
                htmlFor="feedback-admin-note"
                className="ds-sm"
                style={{ fontWeight: 700, display: 'block', marginBottom: 6 }}
              >
                {t('adminFeedback.adminNote')}
              </label>
              <Textarea
                id="feedback-admin-note"
                rows={3}
                value={current.note}
                placeholder={t('adminFeedback.adminNotePlaceholder')}
                onChange={(e) => setDraft({ ...current, note: e.target.value })}
              />
            </div>

            <div>
              <Button onClick={save} disabled={updateStatus.isPending}>
                {updateStatus.isPending ? <Spinner size="sm" /> : t('adminFeedback.save')}
              </Button>
            </div>
          </div>
        )}
      </DialogContent>
    </Dialog>
  )
}
