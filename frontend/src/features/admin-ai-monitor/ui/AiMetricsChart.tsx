import { BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer, CartesianGrid } from 'recharts'
import type { AiStats } from '@/entities/admin'

interface AiMetricsChartProps {
  stats: AiStats
}

export function AiMetricsChart({ stats }: AiMetricsChartProps) {
  const callTypeData = stats.byCallType.map((ct) => ({
    name: ct.callType.replace(/_/g, ' '),
    count: ct.count,
    avgMs: Math.round(ct.avgDurationMs),
    errors: ct.errorCount,
  }))

  return (
    <div className="grid gap-4 md:grid-cols-2">
      {/* Summary stats */}
      <div className="rounded-lg border bg-card p-5 shadow-sm">
        <h3 className="mb-4 text-sm font-semibold text-muted-foreground uppercase tracking-wide">
          Overview (last 24h)
        </h3>
        <div className="grid grid-cols-2 gap-4">
          <div>
            <p className="text-2xl font-bold">{stats.totalCalls}</p>
            <p className="text-xs text-muted-foreground">Total calls</p>
          </div>
          <div>
            <p className="text-2xl font-bold text-green-600">{stats.successfulCalls}</p>
            <p className="text-xs text-muted-foreground">Successful</p>
          </div>
          <div>
            <p className="text-2xl font-bold text-red-500">{stats.failedCalls}</p>
            <p className="text-xs text-muted-foreground">Failed</p>
          </div>
          <div>
            <p className="text-2xl font-bold">{Math.round(stats.averageResponseTimeMs)}ms</p>
            <p className="text-xs text-muted-foreground">Avg response</p>
          </div>
          <div>
            <p className="text-2xl font-bold">{stats.errorRatePercent.toFixed(1)}%</p>
            <p className="text-xs text-muted-foreground">Error rate</p>
          </div>
          <div>
            <p className="text-2xl font-bold">{stats.fallbackCount}</p>
            <p className="text-xs text-muted-foreground">Fallbacks</p>
          </div>
        </div>
      </div>

      {/* Calls by type */}
      <div className="rounded-lg border bg-card p-5 shadow-sm">
        <h3 className="mb-4 text-sm font-semibold text-muted-foreground uppercase tracking-wide">
          Calls by type
        </h3>
        {callTypeData.length === 0 ? (
          <p className="text-sm text-muted-foreground">No data</p>
        ) : (
          <ResponsiveContainer width="100%" height={180}>
            <BarChart data={callTypeData} margin={{ top: 0, right: 0, left: -20, bottom: 0 }}>
              <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
              <XAxis dataKey="name" tick={{ fontSize: 11 }} />
              <YAxis tick={{ fontSize: 11 }} />
              <Tooltip />
              <Bar dataKey="count" fill="hsl(var(--primary))" radius={[3, 3, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
        )}
      </div>
    </div>
  )
}
