import { useState } from 'react'
import {
  Button,
  Input,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Spinner,
  Checkbox,
} from '@/shared/ui'
import { useAiStats, useAiStatus, useAiLogs } from '@/entities/admin'
import type { AiLogsParams } from '@/entities/admin'
import { AiMetricsChart, AiLogTable } from '@/features/admin-ai-monitor'

const STATUS_COLORS: Record<string, string> = {
  healthy: 'bg-green-100 text-green-700 border-green-300',
  degraded: 'bg-yellow-100 text-yellow-700 border-yellow-300',
  unknown: 'bg-gray-100 text-gray-500 border-gray-300',
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
    <div className="p-8">
      <h1 className="mb-6 text-2xl font-bold">AI Monitor</h1>

      {/* Provider status */}
      {providerStatus && providerStatus.length > 0 && (
        <div className="mb-6 flex flex-wrap gap-3">
          {providerStatus.map((p) => (
            <div
              key={p.provider}
              className={`flex items-center gap-2 rounded-full border px-3 py-1 text-sm font-medium ${STATUS_COLORS[p.status] ?? STATUS_COLORS.unknown}`}
            >
              <span className="h-2 w-2 rounded-full bg-current opacity-70" />
              {p.provider}: {p.status}
              <span className="text-xs opacity-70">
                ({p.recentSuccessRatePercent.toFixed(0)}% ok)
              </span>
            </div>
          ))}
        </div>
      )}

      {/* Stats charts */}
      <div className="mb-6">
        {statsLoading ? (
          <div className="flex justify-center py-8">
            <Spinner size="lg" />
          </div>
        ) : stats ? (
          <AiMetricsChart stats={stats} />
        ) : null}
      </div>

      {/* Log filters */}
      <div className="mb-4 flex flex-wrap items-center gap-3">
        <Select
          value={provider}
          onValueChange={(v) => {
            setProvider(!v || v === 'all' ? '' : v)
            setPage(1)
          }}
        >
          <SelectTrigger className="w-36">
            <SelectValue placeholder="All providers" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All providers</SelectItem>
            <SelectItem value="Ollama">Ollama</SelectItem>
            <SelectItem value="OpenAI">OpenAI</SelectItem>
          </SelectContent>
        </Select>
        <Input
          placeholder="Call type…"
          value={callType}
          onChange={(e) => {
            setCallType(e.target.value)
            setPage(1)
          }}
          className="w-40"
        />
        <label className="flex cursor-pointer items-center gap-2 text-sm">
          <Checkbox
            checked={successOnly}
            onCheckedChange={(v) => {
              setSuccessOnly(!!v)
              setPage(1)
            }}
          />
          Success only
        </label>
      </div>

      {/* Log table */}
      <AiLogTable logs={logsData?.items ?? []} isLoading={logsLoading} />

      {/* Pagination */}
      {logsData && logsData.totalPages > 1 && (
        <div className="mt-4 flex items-center justify-center gap-2">
          <Button
            variant="outline"
            size="sm"
            disabled={!logsData.hasPreviousPage}
            onClick={() => setPage((p) => p - 1)}
          >
            Previous
          </Button>
          <span className="text-sm text-muted-foreground">
            {page} / {logsData.totalPages}
          </span>
          <Button
            variant="outline"
            size="sm"
            disabled={!logsData.hasNextPage}
            onClick={() => setPage((p) => p + 1)}
          >
            Next
          </Button>
        </div>
      )}
    </div>
  )
}
