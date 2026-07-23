import { useState } from 'react'
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
import { useAuditLogs } from '@/entities/admin'
import type { AuditLogsParams } from '@/entities/admin'

const PAGE_SIZE = 50

// Every action currently written by the backend (see IAuditService call sites).
const ACTIONS = [
  'suspend_user',
  'restore_user',
  'delete_user',
  'change_user_role',
  'impersonate_user',
  'update_system_setting',
  'add_language',
  'toggle_language',
  'update_feedback_status',
  'verify_user_email',
] as const

const ACTION_COLORS: Record<string, string> = {
  suspend_user: 'var(--warning)',
  restore_user: 'var(--success)',
  delete_user: 'var(--danger)',
  change_user_role: 'var(--warning)',
  impersonate_user: 'var(--danger)',
}

// jsonb values arrive JSON-encoded ("\"active\"") — unwrap plain strings for display.
function displayValue(raw: string | null): string {
  if (raw === null) return '—'
  try {
    const parsed: unknown = JSON.parse(raw)
    return typeof parsed === 'string' ? parsed : raw
  } catch {
    return raw
  }
}

export function AdminAuditPage() {
  const [page, setPage] = useState(1)
  const [action, setAction] = useState('')

  const params: AuditLogsParams = {
    page,
    pageSize: PAGE_SIZE,
    action: action || undefined,
  }
  const { data, isLoading } = useAuditLogs(params)
  const logs = data?.items ?? []

  return (
    <div>
      <h1 className="ds-h2" style={{ margin: '0 0 20px' }}>
        Audit Log
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
          value={action || 'all'}
          onValueChange={(v) => {
            setAction(v === 'all' ? '' : v)
            setPage(1)
          }}
          triggerStyle={{ width: '100%', maxWidth: 220 }}
          options={[
            { value: 'all', label: 'All actions' },
            ...ACTIONS.map((a) => ({ value: a, label: a })),
          ]}
        />
        {data && (
          <span style={{ color: 'var(--fg-4)', fontSize: 12, fontWeight: 600 }}>
            {data.totalCount} entries
          </span>
        )}
      </div>

      {/* Table */}
      {isLoading ? (
        <div className="flex justify-center py-8">
          <Spinner size="lg" />
        </div>
      ) : logs.length === 0 ? (
        <div className="flex flex-col items-center gap-2 py-8">
          <Mascot pose="scientist" size={96} />
          <p className="text-center text-sm text-muted-foreground">No audit entries found.</p>
        </div>
      ) : (
        <div className="overflow-auto rounded-lg border">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Time</TableHead>
                <TableHead>Admin</TableHead>
                <TableHead>Action</TableHead>
                <TableHead>Target</TableHead>
                <TableHead>Change</TableHead>
                <TableHead>IP</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {logs.map((log) => (
                <TableRow key={log.id}>
                  <TableCell className="text-xs whitespace-nowrap text-muted-foreground">
                    {formatDate(log.createdAt)}
                  </TableCell>
                  <TableCell className="max-w-48 truncate text-xs">
                    {log.adminEmail ?? log.adminId}
                  </TableCell>
                  <TableCell>
                    <Badge
                      variant="outline"
                      style={{ color: ACTION_COLORS[log.action] ?? 'var(--fg-2)' }}
                    >
                      {log.action}
                    </Badge>
                  </TableCell>
                  <TableCell className="max-w-56 truncate font-mono text-xs text-muted-foreground">
                    {log.targetType ? `${log.targetType}: ${log.targetId ?? ''}` : '—'}
                  </TableCell>
                  <TableCell className="max-w-64 truncate text-xs">
                    {log.oldValue !== null || log.newValue !== null ? (
                      <>
                        {log.oldValue !== null && (
                          <span className="text-muted-foreground">
                            {displayValue(log.oldValue)} →{' '}
                          </span>
                        )}
                        <span>{displayValue(log.newValue)}</span>
                      </>
                    ) : (
                      '—'
                    )}
                  </TableCell>
                  <TableCell className="font-mono text-xs text-muted-foreground">
                    {log.ipAddress ?? '—'}
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
            ← Previous
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
            Next →
          </button>
        </div>
      )}
    </div>
  )
}
