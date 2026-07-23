import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Star } from 'lucide-react'
import {
  Badge,
  LxSelect,
  Mascot,
  Spinner,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/shared/ui'
import { formatDate } from '@/shared/lib'
import { useAdminFeedback, type AdminFeedbackParams } from '@/entities/feedback'
import { FeedbackDetailDialog } from './FeedbackDetailDialog'

const PAGE_SIZE = 50

const TYPES = ['suggestion', 'bug', 'review', 'question'] as const
const STATUSES = ['new', 'in_progress', 'resolved'] as const

const TYPE_COLORS: Record<string, string> = {
  bug: 'var(--danger)',
  review: 'var(--warning)',
  suggestion: 'var(--accent-color)',
}

const STATUS_COLORS: Record<string, string> = {
  new: 'var(--accent-color)',
  in_progress: 'var(--warning)',
  resolved: 'var(--success)',
}

export function AdminFeedbackPage() {
  const { t } = useTranslation()

  const [page, setPage] = useState(1)
  const [type, setType] = useState('')
  const [status, setStatus] = useState('')
  const [search, setSearch] = useState('')
  const [openId, setOpenId] = useState<string | null>(null)

  const params: AdminFeedbackParams = {
    page,
    pageSize: PAGE_SIZE,
    type: type || undefined,
    status: status || undefined,
    search: search.trim() || undefined,
  }
  const { data, isLoading } = useAdminFeedback(params)
  const items = data?.items ?? []

  return (
    <div>
      <h1 className="ds-h2" style={{ margin: '0 0 20px' }}>
        {t('adminFeedback.title')}
      </h1>

      {/* Filters */}
      <div
        style={{
          display: 'flex',
          flexWrap: 'wrap',
          alignItems: 'center',
          gap: 10,
          marginBottom: 14,
        }}
      >
        <LxSelect
          value={type || 'all'}
          onValueChange={(v) => {
            setType(v === 'all' ? '' : v)
            setPage(1)
          }}
          triggerStyle={{ width: '100%', maxWidth: 180 }}
          options={[
            { value: 'all', label: t('adminFeedback.allTypes') },
            ...TYPES.map((v) => ({
              value: v,
              label: t(`feedback.type${v[0].toUpperCase()}${v.slice(1)}`),
            })),
          ]}
        />
        <LxSelect
          value={status || 'all'}
          onValueChange={(v) => {
            setStatus(v === 'all' ? '' : v)
            setPage(1)
          }}
          triggerStyle={{ width: '100%', maxWidth: 180 }}
          options={[
            { value: 'all', label: t('adminFeedback.allStatuses') },
            ...STATUSES.map((v) => ({ value: v, label: t(`adminFeedback.status.${v}`) })),
          ]}
        />
        <input
          type="search"
          className="lx-input"
          style={{ maxWidth: 260 }}
          placeholder={t('adminFeedback.searchPlaceholder')}
          aria-label={t('adminFeedback.searchPlaceholder')}
          value={search}
          onChange={(e) => {
            setSearch(e.target.value)
            setPage(1)
          }}
        />
        {data && (
          <span style={{ color: 'var(--fg-4)', fontSize: 12, fontWeight: 600 }}>
            {t('adminFeedback.entries', { count: data.totalCount })}
          </span>
        )}
      </div>

      {/* Table */}
      {isLoading ? (
        <div className="flex justify-center py-8">
          <Spinner size="lg" />
        </div>
      ) : items.length === 0 ? (
        <div className="flex flex-col items-center gap-2 py-8">
          <Mascot pose="scientist" size={96} />
          <p className="text-center text-sm text-muted-foreground">{t('adminFeedback.empty')}</p>
        </div>
      ) : (
        <div className="overflow-auto rounded-lg border">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>{t('adminFeedback.colTicket')}</TableHead>
                <TableHead>{t('adminFeedback.colDate')}</TableHead>
                <TableHead>{t('adminFeedback.colType')}</TableHead>
                <TableHead>{t('adminFeedback.colSubject')}</TableHead>
                <TableHead>{t('adminFeedback.colCategory')}</TableHead>
                <TableHead>{t('adminFeedback.colRating')}</TableHead>
                <TableHead>{t('adminFeedback.colStatus')}</TableHead>
                <TableHead>{t('adminFeedback.colAuthor')}</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {items.map((item) => (
                <TableRow
                  key={item.id}
                  tabIndex={0}
                  role="button"
                  className="cursor-pointer"
                  onClick={() => setOpenId(item.id)}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter' || e.key === ' ') {
                      e.preventDefault()
                      setOpenId(item.id)
                    }
                  }}
                >
                  <TableCell className="font-mono text-xs whitespace-nowrap">
                    {item.ticketCode}
                  </TableCell>
                  <TableCell className="text-xs whitespace-nowrap text-muted-foreground">
                    {formatDate(item.createdAt)}
                  </TableCell>
                  <TableCell>
                    <Badge
                      variant="outline"
                      style={{ color: TYPE_COLORS[item.type] ?? 'var(--fg-2)' }}
                    >
                      {item.type}
                    </Badge>
                  </TableCell>
                  <TableCell className="max-w-64 truncate text-xs">
                    {item.subject}
                    {item.attachmentCount > 0 && (
                      <span style={{ color: 'var(--fg-4)' }}> · 📎{item.attachmentCount}</span>
                    )}
                  </TableCell>
                  <TableCell className="text-xs text-muted-foreground">
                    {item.category ?? '—'}
                  </TableCell>
                  <TableCell className="text-xs">
                    {item.rating != null ? (
                      <span style={{ display: 'inline-flex', alignItems: 'center', gap: 3 }}>
                        {item.rating}
                        <Star style={{ width: 12, height: 12 }} fill="currentColor" />
                      </span>
                    ) : (
                      '—'
                    )}
                  </TableCell>
                  <TableCell>
                    <Badge
                      variant="outline"
                      style={{ color: STATUS_COLORS[item.status] ?? 'var(--fg-2)' }}
                    >
                      {t(`adminFeedback.status.${item.status}`)}
                    </Badge>
                  </TableCell>
                  <TableCell className="max-w-48 truncate text-xs text-muted-foreground">
                    {item.userEmail ?? '—'}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}

      {/* Pagination */}
      {data && data.totalPages > 1 && (
        <div
          style={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            gap: 12,
            marginTop: 16,
          }}
        >
          <button
            className="lx-btn-secondary"
            style={{ padding: '6px 16px', fontSize: 12 }}
            disabled={!data.hasPreviousPage}
            onClick={() => setPage((p) => p - 1)}
          >
            ← {t('adminFeedback.prev')}
          </button>
          <span style={{ color: 'var(--fg-3)', fontSize: 12, fontWeight: 600 }}>
            {page} / {data.totalPages}
          </span>
          <button
            className="lx-btn-secondary"
            style={{ padding: '6px 16px', fontSize: 12 }}
            disabled={!data.hasNextPage}
            onClick={() => setPage((p) => p + 1)}
          >
            {t('adminFeedback.next')} →
          </button>
        </div>
      )}

      <FeedbackDetailDialog feedbackId={openId} onClose={() => setOpenId(null)} />
    </div>
  )
}
