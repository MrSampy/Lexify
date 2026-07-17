import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import {
  PieChart,
  Pie,
  Cell,
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
import { ROUTES } from '@/shared/config'
import { Mascot, Spinner } from '@/shared/ui'
import { masteryInfoFor } from '@/shared/lib'
import {
  useActivityStats,
  useMasteryStats,
  useAccuracyStats,
  useForecastStats,
  useProblemWords,
  type DailyReviewCount,
} from '@/entities/user'

/** GitHub-style activity heatmap built from a dense daily-count series. */
function ActivityHeatmap({ days }: { days: DailyReviewCount[] }) {
  const max = days.reduce((m, d) => Math.max(m, d.count), 0)

  // 5 intensity steps (0 = empty). Cheap linear bucketing against the window's busiest day.
  const level = (count: number) => {
    if (count === 0) return 0
    if (max <= 1) return 4
    return Math.min(4, Math.ceil((count / max) * 4))
  }
  const shades = [
    'var(--bg-3)',
    'color-mix(in srgb, var(--accent-color) 30%, var(--bg-3))',
    'color-mix(in srgb, var(--accent-color) 55%, var(--bg-3))',
    'color-mix(in srgb, var(--accent-color) 78%, var(--bg-3))',
    'var(--accent-color)',
  ]

  return (
    <div style={{ display: 'flex', flexWrap: 'wrap', gap: 4 }}>
      {days.map((d) => (
        <div
          key={d.date}
          title={`${d.date}: ${d.count}`}
          style={{
            width: 14,
            height: 14,
            borderRadius: 3,
            background: shades[level(d.count)],
            border: '1px solid var(--line-1)',
          }}
        />
      ))}
    </div>
  )
}

function SectionCard({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="lx-card" style={{ padding: 24 }}>
      <h2 className="ds-h4" style={{ margin: '0 0 16px' }}>
        {title}
      </h2>
      {children}
    </div>
  )
}

export function StatsPage() {
  const { t } = useTranslation()
  const { data: activity, isLoading: activityLoading } = useActivityStats(90)
  const { data: mastery, isLoading: masteryLoading } = useMasteryStats()
  const { data: accuracy, isLoading: accuracyLoading } = useAccuracyStats(30)
  const { data: forecast, isLoading: forecastLoading } = useForecastStats(14)
  const { data: problemWords, isLoading: problemLoading } = useProblemWords(20)

  const masteryData = mastery
    ? (['new', 'learning', 'young', 'mature'] as const).map((level) => ({
        level,
        name: t(masteryInfoFor(level).labelKey),
        value: mastery[level],
        color: masteryInfoFor(level).color,
      }))
    : []
  const masteryTotal = masteryData.reduce((s, d) => s + d.value, 0)

  const accuracyData = (accuracy?.days ?? []).map((d) => ({
    date: d.date.slice(5), // MM-DD
    accuracy: d.total > 0 ? Math.round((d.correct / d.total) * 100) : 0,
  }))

  const forecastData = (forecast?.days ?? []).map((d) => ({
    date: d.date.slice(5), // MM-DD
    count: d.count,
  }))
  const forecastTotal = forecastData.reduce((s, d) => s + d.count, 0)

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 20 }}>
      <div>
        <h1 className="ds-h2" style={{ margin: '0 0 6px' }}>
          {t('stats.title')}
        </h1>
        <p className="ds-body" style={{ margin: 0, color: 'var(--fg-3)' }}>
          {t('stats.subtitle')}
        </p>
      </div>

      {/* Streak + activity */}
      <SectionCard title={t('stats.activityTitle')}>
        {activityLoading ? (
          <div style={{ display: 'flex', justifyContent: 'center', padding: 24 }}>
            <Spinner />
          </div>
        ) : (
          <div style={{ display: 'flex', flexDirection: 'column', gap: 18 }}>
            <div style={{ display: 'flex', gap: 28, flexWrap: 'wrap' }}>
              <div>
                <div
                  style={{
                    fontSize: 34,
                    fontWeight: 800,
                    color: 'var(--accent-color)',
                    fontFamily: 'var(--font-display)',
                    lineHeight: 1,
                  }}
                >
                  🔥 {activity?.currentStreak ?? 0}
                </div>
                <div
                  className="ds-sm"
                  style={{ color: 'var(--fg-3)', marginTop: 4, fontWeight: 600 }}
                >
                  {t('stats.currentStreak')}
                </div>
              </div>
              <div>
                <div
                  style={{
                    fontSize: 34,
                    fontWeight: 800,
                    color: 'var(--fg-2)',
                    fontFamily: 'var(--font-display)',
                    lineHeight: 1,
                  }}
                >
                  {activity?.longestStreak ?? 0}
                </div>
                <div
                  className="ds-sm"
                  style={{ color: 'var(--fg-3)', marginTop: 4, fontWeight: 600 }}
                >
                  {t('stats.longestStreak')}
                </div>
              </div>
              <div>
                <div
                  style={{
                    fontSize: 34,
                    fontWeight: 800,
                    color: 'var(--fg-2)',
                    fontFamily: 'var(--font-display)',
                    lineHeight: 1,
                  }}
                >
                  {activity?.totalReviews ?? 0}
                </div>
                <div
                  className="ds-sm"
                  style={{ color: 'var(--fg-3)', marginTop: 4, fontWeight: 600 }}
                >
                  {t('stats.totalReviews', { days: 90 })}
                </div>
              </div>
            </div>
            {activity && <ActivityHeatmap days={activity.days} />}
          </div>
        )}
      </SectionCard>

      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))',
          gap: 20,
        }}
      >
        {/* Mastery distribution */}
        <SectionCard title={t('stats.masteryTitle')}>
          {masteryLoading ? (
            <div style={{ display: 'flex', justifyContent: 'center', padding: 24 }}>
              <Spinner />
            </div>
          ) : masteryTotal === 0 ? (
            <p className="ds-sm" style={{ color: 'var(--fg-3)' }}>
              {t('stats.noData')}
            </p>
          ) : (
            <div style={{ display: 'flex', alignItems: 'center', gap: 16, flexWrap: 'wrap' }}>
              <ResponsiveContainer width={160} height={160}>
                <PieChart>
                  <Pie
                    data={masteryData}
                    dataKey="value"
                    nameKey="name"
                    innerRadius={45}
                    outerRadius={75}
                    paddingAngle={2}
                  >
                    {masteryData.map((d) => (
                      <Cell key={d.level} fill={d.color} stroke="var(--bg-2)" />
                    ))}
                  </Pie>
                  <Tooltip />
                </PieChart>
              </ResponsiveContainer>
              <div
                style={{ display: 'flex', flexDirection: 'column', gap: 8, flex: 1, minWidth: 140 }}
              >
                {masteryData.map((d) => (
                  <div key={d.level} style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                    <span
                      style={{
                        width: 12,
                        height: 12,
                        borderRadius: 3,
                        background: d.color,
                        flexShrink: 0,
                      }}
                    />
                    <span className="ds-sm" style={{ color: 'var(--fg-2)', flex: 1 }}>
                      {d.name}
                    </span>
                    <span className="ds-sm" style={{ color: 'var(--fg-3)', fontWeight: 700 }}>
                      {d.value}
                    </span>
                  </div>
                ))}
              </div>
            </div>
          )}
        </SectionCard>

        {/* Review load forecast */}
        <SectionCard title={t('stats.forecastTitle')}>
          {forecastLoading ? (
            <div style={{ display: 'flex', justifyContent: 'center', padding: 24 }}>
              <Spinner />
            </div>
          ) : forecastTotal === 0 ? (
            <p className="ds-sm" style={{ color: 'var(--fg-3)' }}>
              {t('stats.noData')}
            </p>
          ) : (
            <ResponsiveContainer width="100%" height={180}>
              <BarChart data={forecastData} margin={{ top: 5, right: 8, left: -20, bottom: 0 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="var(--line-1)" />
                <XAxis dataKey="date" tick={{ fontSize: 11 }} />
                <YAxis allowDecimals={false} tick={{ fontSize: 11 }} />
                <Tooltip cursor={{ fill: 'var(--bg-3)' }} />
                <Bar dataKey="count" fill="var(--accent-color)" radius={[3, 3, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          )}
        </SectionCard>

        {/* Accuracy trend */}
        <SectionCard title={t('stats.accuracyTitle')}>
          {accuracyLoading ? (
            <div style={{ display: 'flex', justifyContent: 'center', padding: 24 }}>
              <Spinner />
            </div>
          ) : accuracyData.length === 0 ? (
            <p className="ds-sm" style={{ color: 'var(--fg-3)' }}>
              {t('stats.noData')}
            </p>
          ) : (
            <ResponsiveContainer width="100%" height={180}>
              <LineChart data={accuracyData} margin={{ top: 5, right: 8, left: -20, bottom: 0 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="var(--line-1)" />
                <XAxis dataKey="date" tick={{ fontSize: 11 }} />
                <YAxis domain={[0, 100]} tick={{ fontSize: 11 }} />
                <Tooltip formatter={(v) => `${v}%`} />
                <Line
                  type="monotone"
                  dataKey="accuracy"
                  stroke="var(--accent-color)"
                  strokeWidth={2}
                  dot={{ r: 2 }}
                />
              </LineChart>
            </ResponsiveContainer>
          )}
        </SectionCard>
      </div>

      {/* Problem words: leeches + confidence-flagged */}
      <SectionCard title={t('stats.problemWordsTitle')}>
        {problemLoading ? (
          <div style={{ display: 'flex', justifyContent: 'center', padding: 24 }}>
            <Spinner />
          </div>
        ) : !problemWords || problemWords.length === 0 ? (
          <p className="ds-sm" style={{ color: 'var(--fg-3)' }}>
            {t('stats.noProblemWords')}
          </p>
        ) : (
          <div style={{ display: 'flex', flexDirection: 'column' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: 10, margin: '0 0 10px' }}>
              <Mascot pose="confused" size={48} />
              <p className="ds-sm" style={{ color: 'var(--fg-4)', margin: 0 }}>
                {t('stats.problemWordsHint')}
              </p>
            </div>
            {problemWords.map((w) => (
              <Link
                key={w.wordId}
                to={ROUTES.BLOCK_DETAIL(w.blockId)}
                style={{
                  display: 'flex',
                  alignItems: 'center',
                  gap: 12,
                  padding: '8px 4px',
                  borderTop: '1px solid var(--line-1)',
                  textDecoration: 'none',
                }}
              >
                <span
                  style={{
                    flex: 1,
                    minWidth: 0,
                    color: 'var(--fg-1)',
                    fontWeight: 600,
                    fontSize: 14,
                    overflow: 'hidden',
                    textOverflow: 'ellipsis',
                    whiteSpace: 'nowrap',
                  }}
                >
                  {w.term}
                  <span style={{ color: 'var(--fg-4)', fontWeight: 400 }}> — {w.translation}</span>
                </span>
                {w.lapseCount > 0 && (
                  <span
                    className="ds-sm"
                    style={{ color: 'var(--danger)', fontWeight: 700, flexShrink: 0 }}
                  >
                    {t('stats.lapses', { count: w.lapseCount })}
                  </span>
                )}
                <span
                  className="ds-sm"
                  style={{
                    color: 'var(--fg-4)',
                    flexShrink: 0,
                    maxWidth: 140,
                    overflow: 'hidden',
                    textOverflow: 'ellipsis',
                    whiteSpace: 'nowrap',
                  }}
                >
                  {w.blockTitle}
                </span>
              </Link>
            ))}
          </div>
        )}
      </SectionCard>
    </div>
  )
}
