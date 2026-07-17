import {
  LineChart,
  Line,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  Tooltip,
  ResponsiveContainer,
  CartesianGrid,
} from 'recharts'
import { Spinner } from '@/shared/ui'
import {
  useDashboardStats,
  useRegistrationsChart,
  useAiCallsChart,
  useSystemHealth,
} from '@/entities/admin'

const HEALTH_COLORS: Record<string, string> = {
  Healthy: 'var(--success)',
  Degraded: 'var(--warning)',
  Unhealthy: 'var(--danger)',
}

function hoursAgo(iso: string): number {
  return Math.floor((Date.now() - new Date(iso).getTime()) / 3_600_000)
}

/** Health-check dots, failed-jobs count, backup age, and a Hangfire link, in one quiet strip. */
function SystemHealthStrip() {
  const { data: health } = useSystemHealth()
  if (!health) return null

  const backupText = !health.backupMonitored
    ? null
    : health.lastBackupAt
      ? `backup ${hoursAgo(health.lastBackupAt)}h ago`
      : 'no backups found'
  const backupStale = health.backupMonitored
    ? !health.lastBackupAt || hoursAgo(health.lastBackupAt!) > 26
    : false

  return (
    <div
      style={{ display: 'flex', flexWrap: 'wrap', alignItems: 'center', gap: 10, marginBottom: 20 }}
    >
      {health.checks.map((c) => (
        <span
          key={c.name}
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: 8,
            padding: '6px 14px',
            background: 'var(--bg-2)',
            border: '1px solid var(--line-2)',
            borderRadius: 999,
            fontSize: 12,
            fontWeight: 600,
            color: 'var(--fg-2)',
          }}
        >
          <span
            style={{
              width: 7,
              height: 7,
              borderRadius: '50%',
              background: HEALTH_COLORS[c.status] ?? 'var(--fg-4)',
              flexShrink: 0,
            }}
          />
          {c.name}
        </span>
      ))}
      {health.failedJobs !== null && (
        <span
          style={{
            padding: '6px 14px',
            background: 'var(--bg-2)',
            border: '1px solid var(--line-2)',
            borderRadius: 999,
            fontSize: 12,
            fontWeight: 600,
            color: health.failedJobs > 0 ? 'var(--danger)' : 'var(--fg-2)',
          }}
        >
          {health.failedJobs} failed jobs
        </span>
      )}
      {backupText && (
        <span
          style={{
            padding: '6px 14px',
            background: 'var(--bg-2)',
            border: '1px solid var(--line-2)',
            borderRadius: 999,
            fontSize: 12,
            fontWeight: 600,
            color: backupStale ? 'var(--danger)' : 'var(--fg-2)',
          }}
        >
          {backupText}
        </span>
      )}
      <a
        href="/hangfire"
        target="_blank"
        rel="noreferrer"
        style={{
          padding: '6px 14px',
          borderRadius: 999,
          border: '1px solid var(--line-2)',
          background: 'var(--bg-2)',
          fontSize: 12,
          fontWeight: 700,
          color: 'var(--accent-color)',
          textDecoration: 'none',
        }}
      >
        Hangfire →
      </a>
    </div>
  )
}

const chartTooltipStyle = {
  backgroundColor: 'var(--bg-3)',
  border: '1px solid var(--line-2)',
  borderRadius: 'var(--r-xs)',
  color: 'var(--fg-1)',
  fontFamily: 'var(--font-body)',
  fontSize: 11,
}

function StatCard({ label, value }: { label: string; value: number | string }) {
  return (
    <div className="rounded-[var(--r-md)] border border-[var(--line-2)] bg-[var(--bg-2)] px-5 py-[18px]">
      <div className="mb-1.5 text-4xl leading-none font-bold tracking-[-0.02em] text-[var(--fg-1)] [font-family:var(--font-display)]">
        {value}
      </div>
      <div className="ds-sm text-[11px] font-semibold text-[var(--fg-3)]">{label}</div>
    </div>
  )
}

export function AdminDashboardPage() {
  const { data: stats, isLoading: statsLoading } = useDashboardStats()
  const { data: registrations, isLoading: regLoading } = useRegistrationsChart(30)
  const { data: aiCalls, isLoading: aiLoading } = useAiCallsChart(24)

  const regData = (registrations ?? []).map((d) => ({ date: d.date, count: d.count }))
  const aiData = (aiCalls ?? []).map((d) => ({
    hour: new Date(d.hourStart).getHours() + 'h',
    count: d.count,
  }))

  return (
    <div>
      <h1 className="ds-h2" style={{ margin: '0 0 24px' }}>
        Dashboard
      </h1>

      <SystemHealthStrip />

      {statsLoading ? (
        <div style={{ display: 'flex', justifyContent: 'center', padding: '60px 0' }}>
          <Spinner size="lg" />
        </div>
      ) : stats ? (
        <>
          {/* Stat grid */}
          <div className="mb-3 grid grid-cols-3 gap-3">
            <StatCard label="total users" value={stats.totalUsers} />
            <StatCard label="active (7d)" value={stats.activeUsersLast7Days} />
            <StatCard label="active (30d)" value={stats.activeUsersLast30Days} />
            <StatCard label="total words" value={stats.totalWords} />
            <StatCard label="total blocks" value={stats.totalWordBlocks} />
            <StatCard label="total tests" value={stats.totalTests} />
          </div>
          <div className="mb-6 grid grid-cols-2 gap-3">
            <StatCard label="ai calls (24h)" value={stats.aiCallsLast24Hours} />
            <StatCard label="ai calls (7d)" value={stats.aiCallsLast7Days} />
          </div>
        </>
      ) : null}

      {/* Charts */}
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))',
          gap: 16,
        }}
      >
        <div
          style={{
            background: 'var(--bg-2)',
            border: '1px solid var(--line-2)',
            borderRadius: 'var(--r-md)',
            padding: '18px 20px',
          }}
        >
          <div
            style={{
              color: 'var(--fg-3)',
              fontSize: 11,
              fontWeight: 700,
              marginBottom: 16,
              textTransform: 'uppercase',
              letterSpacing: '0.1em',
            }}
          >
            Registrations — 30d
          </div>
          {regLoading ? (
            <div
              style={{
                display: 'flex',
                justifyContent: 'center',
                height: 160,
                alignItems: 'center',
              }}
            >
              <Spinner />
            </div>
          ) : regData.length === 0 ? (
            <p className="ds-sm" style={{ color: 'var(--fg-4)' }}>
              No data
            </p>
          ) : (
            <ResponsiveContainer width="100%" height={180}>
              <LineChart data={regData} margin={{ top: 0, right: 0, left: -20, bottom: 0 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="var(--line-1)" />
                <XAxis
                  dataKey="date"
                  tick={{ fontSize: 9, fill: 'var(--fg-4)', fontFamily: 'var(--font-body)' }}
                  tickFormatter={(v: string) => v.slice(5)}
                />
                <YAxis
                  tick={{ fontSize: 9, fill: 'var(--fg-4)', fontFamily: 'var(--font-body)' }}
                  allowDecimals={false}
                />
                <Tooltip contentStyle={chartTooltipStyle} />
                <Line
                  type="monotone"
                  dataKey="count"
                  stroke="var(--accent-color)"
                  strokeWidth={2}
                  dot={false}
                />
              </LineChart>
            </ResponsiveContainer>
          )}
        </div>

        <div
          style={{
            background: 'var(--bg-2)',
            border: '1px solid var(--line-2)',
            borderRadius: 'var(--r-md)',
            padding: '18px 20px',
          }}
        >
          <div
            style={{
              color: 'var(--fg-3)',
              fontSize: 11,
              fontWeight: 700,
              marginBottom: 16,
              textTransform: 'uppercase',
              letterSpacing: '0.1em',
            }}
          >
            AI calls — last 24h
          </div>
          {aiLoading ? (
            <div
              style={{
                display: 'flex',
                justifyContent: 'center',
                height: 160,
                alignItems: 'center',
              }}
            >
              <Spinner />
            </div>
          ) : aiData.length === 0 ? (
            <p className="ds-sm" style={{ color: 'var(--fg-4)' }}>
              No data
            </p>
          ) : (
            <ResponsiveContainer width="100%" height={180}>
              <BarChart data={aiData} margin={{ top: 0, right: 0, left: -20, bottom: 0 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="var(--line-1)" />
                <XAxis
                  dataKey="hour"
                  tick={{ fontSize: 9, fill: 'var(--fg-4)', fontFamily: 'var(--font-body)' }}
                />
                <YAxis
                  tick={{ fontSize: 9, fill: 'var(--fg-4)', fontFamily: 'var(--font-body)' }}
                  allowDecimals={false}
                />
                <Tooltip contentStyle={chartTooltipStyle} />
                <Bar
                  dataKey="count"
                  fill="var(--accent-color)"
                  radius={[3, 3, 0, 0]}
                  opacity={0.8}
                />
              </BarChart>
            </ResponsiveContainer>
          )}
        </div>
      </div>
    </div>
  )
}
