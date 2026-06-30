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
import { useDashboardStats, useRegistrationsChart, useAiCallsChart } from '@/entities/admin'

const chartTooltipStyle = {
  backgroundColor: 'var(--bg-3)',
  border: '1px solid var(--line-2)',
  borderRadius: 6,
  color: 'var(--fg-1)',
  fontFamily: 'var(--font-mono)',
  fontSize: 11,
}

function StatCard({ label, value }: { label: string; value: number | string }) {
  return (
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
          fontFamily: 'var(--font-display)',
          fontWeight: 700,
          fontSize: 36,
          color: 'var(--fg-1)',
          letterSpacing: '-0.02em',
          lineHeight: 1,
          marginBottom: 6,
        }}
      >
        {value}
      </div>
      <div className="ds-code" style={{ color: 'var(--fg-3)', fontSize: 11 }}>
        {label}
      </div>
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
      <div className="eyebrow" style={{ marginBottom: 14 }}>
        ~/admin/dashboard
      </div>
      <h1 className="ds-h2" style={{ margin: '0 0 24px' }}>
        Dashboard
      </h1>

      {statsLoading ? (
        <div style={{ display: 'flex', justifyContent: 'center', padding: '60px 0' }}>
          <Spinner size="lg" />
        </div>
      ) : stats ? (
        <>
          {/* Stat grid */}
          <div
            style={{
              display: 'grid',
              gridTemplateColumns: 'repeat(3, 1fr)',
              gap: 12,
              marginBottom: 12,
            }}
          >
            <StatCard label="total users" value={stats.totalUsers} />
            <StatCard label="active (7d)" value={stats.activeUsersLast7Days} />
            <StatCard label="active (30d)" value={stats.activeUsersLast30Days} />
            <StatCard label="total words" value={stats.totalWords} />
            <StatCard label="total blocks" value={stats.totalWordBlocks} />
            <StatCard label="total tests" value={stats.totalTests} />
          </div>
          <div
            style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12, marginBottom: 24 }}
          >
            <StatCard label="ai calls (24h)" value={stats.aiCallsLast24Hours} />
            <StatCard label="ai calls (7d)" value={stats.aiCallsLast7Days} />
          </div>
        </>
      ) : null}

      {/* Charts */}
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
        <div
          style={{
            background: 'var(--bg-2)',
            border: '1px solid var(--line-2)',
            borderRadius: 'var(--r-md)',
            padding: '18px 20px',
          }}
        >
          <div
            className="ds-code"
            style={{
              color: 'var(--fg-3)',
              fontSize: 11,
              marginBottom: 16,
              textTransform: 'uppercase',
              letterSpacing: '0.1em',
            }}
          >
            // registrations — 30d
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
            <p className="ds-code" style={{ color: 'var(--fg-4)' }}>
              no data
            </p>
          ) : (
            <ResponsiveContainer width="100%" height={180}>
              <LineChart data={regData} margin={{ top: 0, right: 0, left: -20, bottom: 0 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="var(--line-1)" />
                <XAxis
                  dataKey="date"
                  tick={{ fontSize: 9, fill: 'var(--fg-4)', fontFamily: 'var(--font-mono)' }}
                  tickFormatter={(v: string) => v.slice(5)}
                />
                <YAxis
                  tick={{ fontSize: 9, fill: 'var(--fg-4)', fontFamily: 'var(--font-mono)' }}
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
            className="ds-code"
            style={{
              color: 'var(--fg-3)',
              fontSize: 11,
              marginBottom: 16,
              textTransform: 'uppercase',
              letterSpacing: '0.1em',
            }}
          >
            // ai calls — last 24h
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
            <p className="ds-code" style={{ color: 'var(--fg-4)' }}>
              no data
            </p>
          ) : (
            <ResponsiveContainer width="100%" height={180}>
              <BarChart data={aiData} margin={{ top: 0, right: 0, left: -20, bottom: 0 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="var(--line-1)" />
                <XAxis
                  dataKey="hour"
                  tick={{ fontSize: 9, fill: 'var(--fg-4)', fontFamily: 'var(--font-mono)' }}
                />
                <YAxis
                  tick={{ fontSize: 9, fill: 'var(--fg-4)', fontFamily: 'var(--font-mono)' }}
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
