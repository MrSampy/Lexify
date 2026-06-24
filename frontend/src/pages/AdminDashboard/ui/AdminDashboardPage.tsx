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

function StatCard({ label, value }: { label: string; value: number | string }) {
  return (
    <div className="rounded-lg border bg-card p-5 shadow-sm">
      <p className="text-3xl font-bold">{value}</p>
      <p className="mt-1 text-sm text-muted-foreground">{label}</p>
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
    <div className="p-8">
      <h1 className="mb-6 text-2xl font-bold">Dashboard</h1>

      {statsLoading ? (
        <div className="flex justify-center py-16">
          <Spinner size="lg" />
        </div>
      ) : stats ? (
        <>
          {/* Stat cards */}
          <div className="mb-6 grid grid-cols-2 gap-4 lg:grid-cols-3">
            <StatCard label="Total users" value={stats.totalUsers} />
            <StatCard label="Active (7d)" value={stats.activeUsersLast7Days} />
            <StatCard label="Active (30d)" value={stats.activeUsersLast30Days} />
            <StatCard label="Total words" value={stats.totalWords} />
            <StatCard label="Total blocks" value={stats.totalWordBlocks} />
            <StatCard label="Total tests" value={stats.totalTests} />
          </div>

          {/* AI calls summary */}
          <div className="mb-6 grid grid-cols-2 gap-4">
            <StatCard label="AI calls (24h)" value={stats.aiCallsLast24Hours} />
            <StatCard label="AI calls (7d)" value={stats.aiCallsLast7Days} />
          </div>
        </>
      ) : null}

      {/* Charts */}
      <div className="grid gap-6 lg:grid-cols-2">
        {/* Registrations */}
        <div className="rounded-lg border bg-card p-5 shadow-sm">
          <h2 className="mb-4 text-sm font-semibold text-muted-foreground uppercase tracking-wide">
            Registrations (30 days)
          </h2>
          {regLoading ? (
            <div className="flex h-40 items-center justify-center">
              <Spinner />
            </div>
          ) : regData.length === 0 ? (
            <p className="text-sm text-muted-foreground">No data</p>
          ) : (
            <ResponsiveContainer width="100%" height={200}>
              <LineChart data={regData} margin={{ top: 0, right: 0, left: -20, bottom: 0 }}>
                <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
                <XAxis
                  dataKey="date"
                  tick={{ fontSize: 10 }}
                  tickFormatter={(v: string) => v.slice(5)}
                />
                <YAxis tick={{ fontSize: 10 }} allowDecimals={false} />
                <Tooltip />
                <Line
                  type="monotone"
                  dataKey="count"
                  stroke="hsl(var(--primary))"
                  strokeWidth={2}
                  dot={false}
                />
              </LineChart>
            </ResponsiveContainer>
          )}
        </div>

        {/* AI calls chart */}
        <div className="rounded-lg border bg-card p-5 shadow-sm">
          <h2 className="mb-4 text-sm font-semibold text-muted-foreground uppercase tracking-wide">
            AI calls (last 24h)
          </h2>
          {aiLoading ? (
            <div className="flex h-40 items-center justify-center">
              <Spinner />
            </div>
          ) : aiData.length === 0 ? (
            <p className="text-sm text-muted-foreground">No data</p>
          ) : (
            <ResponsiveContainer width="100%" height={200}>
              <BarChart data={aiData} margin={{ top: 0, right: 0, left: -20, bottom: 0 }}>
                <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
                <XAxis dataKey="hour" tick={{ fontSize: 10 }} />
                <YAxis tick={{ fontSize: 10 }} allowDecimals={false} />
                <Tooltip />
                <Bar dataKey="count" fill="hsl(var(--primary))" radius={[3, 3, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          )}
        </div>
      </div>
    </div>
  )
}
