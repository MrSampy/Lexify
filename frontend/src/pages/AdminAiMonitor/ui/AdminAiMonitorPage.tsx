import { useState } from 'react'
import { Spinner } from '@/shared/ui'
import { useAiStats, useAiStatus, useAiLogs } from '@/entities/admin'
import type { AiLogsParams } from '@/entities/admin'
import { AiMetricsChart, AiLogTable } from '@/features/admin-ai-monitor'

const STATUS_DOT: Record<string, string> = {
  healthy: 'var(--success)',
  degraded: 'var(--warning)',
  unknown: 'var(--fg-4)',
}

const PAGE_SIZE = 50

export function AdminAiMonitorPage() {
  const [page, setPage] = useState(1)
  const [provider, setProvider] = useState('')
  const [callType, setCallType] = useState('')
  const [successOnly, setSuccessOnly] = useState(false)

  const { data: stats, isLoading: statsLoading } = useAiStats(24)
  const { data: providerStatus } = useAiStatus()

  const logsParams: AiLogsParams = {
    page,
    pageSize: PAGE_SIZE,
    provider: provider || undefined,
    callType: callType || undefined,
    success: successOnly ? true : undefined,
  }
  const { data: logsData, isLoading: logsLoading } = useAiLogs(logsParams)

  return (
    <div>
      <h1 className="ds-h2" style={{ margin: '0 0 20px' }}>
        AI Monitor
      </h1>

      {/* Provider status */}
      {providerStatus && providerStatus.length > 0 && (
        <div style={{ display: 'flex', flexWrap: 'wrap', gap: 10, marginBottom: 20 }}>
          {providerStatus.map((p) => (
            <div
              key={p.provider}
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: 8,
                padding: '6px 14px',
                background: 'var(--bg-2)',
                border: '1px solid var(--line-2)',
                borderRadius: 999,
              }}
            >
              <span
                style={{
                  width: 7,
                  height: 7,
                  borderRadius: '50%',
                  background: STATUS_DOT[p.status] ?? STATUS_DOT.unknown,
                  flexShrink: 0,
                }}
              />
              <span style={{ color: 'var(--fg-2)', fontSize: 12, fontWeight: 600 }}>
                {p.provider}
              </span>
              <span
                style={{
                  color: STATUS_DOT[p.status] ?? 'var(--fg-4)',
                  fontSize: 11,
                  fontWeight: 600,
                }}
              >
                {p.status} · {p.recentSuccessRatePercent.toFixed(0)}%
              </span>
            </div>
          ))}
        </div>
      )}

      {/* Stats charts */}
      <div style={{ marginBottom: 24 }}>
        {statsLoading ? (
          <div style={{ display: 'flex', justifyContent: 'center', padding: '40px 0' }}>
            <Spinner size="lg" />
          </div>
        ) : stats ? (
          <AiMetricsChart stats={stats} />
        ) : null}
      </div>

      {/* Log filters */}
      <div
        style={{
          display: 'flex',
          flexWrap: 'wrap',
          alignItems: 'center',
          gap: 10,
          marginBottom: 14,
        }}
      >
        <select
          className="lx-input"
          value={provider}
          onChange={(e) => {
            setProvider(e.target.value === 'all' ? '' : e.target.value)
            setPage(1)
          }}
          style={{ width: 150, height: 36, fontSize: 13, cursor: 'pointer' }}
        >
          <option value="all">All providers</option>
          {(providerStatus ?? []).map((p) => (
            <option key={p.provider} value={p.provider}>
              {p.provider}
            </option>
          ))}
        </select>
        <input
          className="lx-input"
          placeholder="call type…"
          value={callType}
          onChange={(e) => {
            setCallType(e.target.value)
            setPage(1)
          }}
          style={{ width: 160, height: 36, fontSize: 13 }}
        />
        <label style={{ display: 'flex', alignItems: 'center', gap: 8, cursor: 'pointer' }}>
          <input
            type="checkbox"
            checked={successOnly}
            onChange={(e) => {
              setSuccessOnly(e.target.checked)
              setPage(1)
            }}
            style={{ accentColor: 'var(--accent-color)', width: 14, height: 14 }}
          />
          <span style={{ color: 'var(--fg-2)', fontSize: 13, fontWeight: 600 }}>Success only</span>
        </label>
      </div>

      {/* Log table */}
      <AiLogTable logs={logsData?.items ?? []} isLoading={logsLoading} />

      {/* Pagination */}
      {logsData && logsData.totalPages > 1 && (
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
            disabled={!logsData.hasPreviousPage}
            onClick={() => setPage((p) => p - 1)}
          >
            ← Previous
          </button>
          <span style={{ color: 'var(--fg-3)', fontSize: 12, fontWeight: 600 }}>
            {page} / {logsData.totalPages}
          </span>
          <button
            className="lx-btn-secondary"
            style={{ padding: '6px 16px', fontSize: 12 }}
            disabled={!logsData.hasNextPage}
            onClick={() => setPage((p) => p + 1)}
          >
            Next →
          </button>
        </div>
      )}
    </div>
  )
}
