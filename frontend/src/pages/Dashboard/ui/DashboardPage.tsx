import { Link } from 'react-router-dom'
import { ROUTES, LANGUAGES } from '@/shared/config'
import { Spinner } from '@/shared/ui'
import { useAuthStore, useUserStats } from '@/entities/user'
import { useBlocks } from '@/entities/block'
import { useTests } from '@/entities/test'
import { ReviewDueBanner } from '@/widgets/ReviewDueBanner'

function StatCard({ label, value }: { label: string; value: number | undefined }) {
  return (
    <div
      style={{
        padding: '18px 20px',
        background: 'var(--bg-2)',
        border: '1px solid var(--line-2)',
        borderRadius: 'var(--r-lg)',
      }}
    >
      <div
        style={{
          fontFamily: 'var(--font-display)',
          fontWeight: 700,
          fontSize: 32,
          color: 'var(--fg-1)',
          letterSpacing: '-0.02em',
        }}
      >
        {value ?? '—'}
      </div>
      <div className="ds-code" style={{ color: 'var(--fg-3)', marginTop: 4 }}>
        {label}
      </div>
    </div>
  )
}

const STATUS_STYLES: Record<string, { bg: string; color: string; border: string }> = {
  ready: { bg: 'var(--success-ghost)', color: 'var(--success)', border: 'rgba(63,214,139,0.3)' },
  generating: {
    bg: 'var(--warning-ghost)',
    color: 'var(--warning)',
    border: 'rgba(245,181,61,0.3)',
  },
  archived: { bg: 'var(--bg-3)', color: 'var(--fg-3)', border: 'var(--line-2)' },
}

export function DashboardPage() {
  const user = useAuthStore((s) => s.user)
  const { data: stats } = useUserStats()
  const { data: blocksPage, isLoading: blocksLoading } = useBlocks({ page: 1, pageSize: 3 })
  const { data: testsPage, isLoading: testsLoading } = useTests(undefined, 1)

  const recentBlocks = blocksPage?.items ?? []
  const recentTests = (testsPage?.items ?? []).slice(0, 3)
  const displayName = user?.displayName ?? user?.email?.split('@')[0] ?? 'there'

  return (
    <div>
      {/* Greeting */}
      <div className="eyebrow" style={{ marginBottom: 12 }}>
        ~/dashboard
      </div>
      <h1 className="ds-h2" style={{ margin: '0 0 6px' }}>
        Welcome back, {displayName}
      </h1>
      <p className="ds-body" style={{ margin: '0 0 24px', color: 'var(--fg-3)' }}>
        Here's where your vocabulary stands today.
      </p>

      {/* Review banner */}
      <div style={{ marginBottom: 26 }}>
        <ReviewDueBanner />
      </div>

      {/* 2 CTA cards */}
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fit, minmax(240px, 1fr))',
          gap: 16,
          marginBottom: 26,
        }}
      >
        <Link to={ROUTES.BLOCKS} style={{ textDecoration: 'none' }}>
          <div
            className="lx-card"
            style={{
              padding: 24,
              cursor: 'pointer',
              transition: 'border-color 0.15s, box-shadow 0.15s',
            }}
            onMouseEnter={(e) => {
              const el = e.currentTarget as HTMLDivElement
              el.style.borderColor = 'var(--accent-line)'
              el.style.boxShadow = 'var(--glow-accent)'
            }}
            onMouseLeave={(e) => {
              const el = e.currentTarget as HTMLDivElement
              el.style.borderColor = 'var(--line-2)'
              el.style.boxShadow = 'none'
            }}
          >
            <div className="ds-code" style={{ color: 'var(--accent-color)', marginBottom: 14 }}>
              ~/blocks
            </div>
            <div className="ds-h3" style={{ marginBottom: 6 }}>
              Word blocks
            </div>
            <p className="ds-sm" style={{ color: 'var(--fg-3)', margin: 0 }}>
              Organize and import vocabulary →
            </p>
          </div>
        </Link>

        <Link to={ROUTES.TESTS} style={{ textDecoration: 'none' }}>
          <div
            className="lx-card"
            style={{
              padding: 24,
              cursor: 'pointer',
              transition: 'border-color 0.15s, box-shadow 0.15s',
            }}
            onMouseEnter={(e) => {
              const el = e.currentTarget as HTMLDivElement
              el.style.borderColor = 'var(--accent-line)'
              el.style.boxShadow = 'var(--glow-accent)'
            }}
            onMouseLeave={(e) => {
              const el = e.currentTarget as HTMLDivElement
              el.style.borderColor = 'var(--line-2)'
              el.style.boxShadow = 'none'
            }}
          >
            <div className="ds-code" style={{ color: 'var(--accent-color)', marginBottom: 14 }}>
              ~/tests
            </div>
            <div className="ds-h3" style={{ marginBottom: 6 }}>
              Tests
            </div>
            <p className="ds-sm" style={{ color: 'var(--fg-3)', margin: 0 }}>
              AI-generated quizzes, 4 question types →
            </p>
          </div>
        </Link>
      </div>

      {/* Stats */}
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fit, minmax(150px, 1fr))',
          gap: 14,
          marginBottom: 36,
        }}
      >
        <StatCard label="blocks" value={stats?.totalBlocks} />
        <StatCard label="words total" value={stats?.totalWords} />
        <StatCard label="answers this week" value={stats?.wordsAnsweredThisWeek} />
        <StatCard label="tests this week" value={stats?.testsCompletedThisWeek} />
      </div>

      {/* Recent blocks */}
      <div style={{ marginBottom: 32 }}>
        <div className="lx-section-head">
          <span className="ds-code" style={{ color: 'var(--accent-color)' }}>
            01
          </span>
          <h2
            style={{
              margin: 0,
              fontFamily: 'var(--font-body)',
              fontWeight: 600,
              fontSize: 13,
              textTransform: 'uppercase',
              letterSpacing: '0.06em',
              color: 'var(--fg-2)',
            }}
          >
            Recent blocks
          </h2>
          <div className="lx-rule" />
          <Link
            to={ROUTES.BLOCKS}
            className="ds-code"
            style={{
              color: 'var(--accent-color)',
              fontSize: 12,
              flexShrink: 0,
              textDecoration: 'none',
            }}
          >
            All blocks →
          </Link>
        </div>

        {blocksLoading ? (
          <div style={{ display: 'flex', justifyContent: 'center', padding: '24px 0' }}>
            <Spinner />
          </div>
        ) : recentBlocks.length === 0 ? (
          <p className="ds-sm" style={{ color: 'var(--fg-3)' }}>
            No blocks yet.{' '}
            <Link to={ROUTES.BLOCKS} style={{ color: 'var(--accent-color)' }}>
              Create your first block →
            </Link>
          </p>
        ) : (
          <div
            style={{
              display: 'grid',
              gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))',
              gap: 14,
            }}
          >
            {recentBlocks.map((block) => {
              const langCode = LANGUAGES[block.languageId]?.code ?? String(block.languageId)
              return (
                <Link
                  key={block.id}
                  to={ROUTES.BLOCK_DETAIL(block.id)}
                  style={{ textDecoration: 'none' }}
                >
                  <div
                    className="lx-card"
                    style={{ padding: 18, cursor: 'pointer', transition: 'border-color 0.15s' }}
                    onMouseEnter={(e) => {
                      ;(e.currentTarget as HTMLDivElement).style.borderColor = 'var(--accent-line)'
                    }}
                    onMouseLeave={(e) => {
                      ;(e.currentTarget as HTMLDivElement).style.borderColor = 'var(--line-2)'
                    }}
                  >
                    <div
                      style={{
                        display: 'flex',
                        justifyContent: 'space-between',
                        alignItems: 'start',
                        gap: 10,
                        marginBottom: 14,
                      }}
                    >
                      <div className="ds-h4" style={{ color: 'var(--fg-1)', fontSize: 15 }}>
                        {block.title}
                      </div>
                      <span
                        style={{
                          fontFamily: 'var(--font-mono)',
                          fontSize: 11,
                          padding: '3px 8px',
                          borderRadius: 'var(--r-sm)',
                          background: 'var(--bg-1)',
                          border: '1px solid var(--line-2)',
                          color: 'var(--fg-2)',
                          flexShrink: 0,
                        }}
                      >
                        {langCode.toUpperCase()}
                      </span>
                    </div>
                    <div className="ds-code" style={{ color: 'var(--fg-3)' }}>
                      {block.wordCount} words
                    </div>
                  </div>
                </Link>
              )
            })}
          </div>
        )}
      </div>

      {/* Recent tests */}
      <div>
        <div className="lx-section-head">
          <span className="ds-code" style={{ color: 'var(--accent-color)' }}>
            02
          </span>
          <h2
            style={{
              margin: 0,
              fontFamily: 'var(--font-body)',
              fontWeight: 600,
              fontSize: 13,
              textTransform: 'uppercase',
              letterSpacing: '0.06em',
              color: 'var(--fg-2)',
            }}
          >
            Recent tests
          </h2>
          <div className="lx-rule" />
          <Link
            to={ROUTES.TESTS}
            className="ds-code"
            style={{
              color: 'var(--accent-color)',
              fontSize: 12,
              flexShrink: 0,
              textDecoration: 'none',
            }}
          >
            All tests →
          </Link>
        </div>

        {testsLoading ? (
          <div style={{ display: 'flex', justifyContent: 'center', padding: '24px 0' }}>
            <Spinner />
          </div>
        ) : recentTests.length === 0 ? (
          <p className="ds-sm" style={{ color: 'var(--fg-3)' }}>
            No tests yet.{' '}
            <Link to={ROUTES.TEST_CREATE} style={{ color: 'var(--accent-color)' }}>
              Create your first test →
            </Link>
          </p>
        ) : (
          <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
            {recentTests.map((test) => {
              const s = STATUS_STYLES[test.status] ?? STATUS_STYLES.archived
              return (
                <div
                  key={test.id}
                  style={{
                    display: 'flex',
                    alignItems: 'center',
                    gap: 18,
                    padding: '16px 20px',
                    background: 'var(--bg-2)',
                    border: '1px solid var(--line-2)',
                    borderRadius: 'var(--r-lg)',
                    flexWrap: 'wrap',
                  }}
                >
                  <div style={{ flex: 1, minWidth: 180 }}>
                    <div
                      className="ds-h4"
                      style={{ color: 'var(--fg-1)', fontSize: 15, marginBottom: 4 }}
                    >
                      {test.title}
                    </div>
                  </div>
                  <span
                    style={{
                      fontFamily: 'var(--font-mono)',
                      fontSize: 11,
                      padding: '4px 10px',
                      borderRadius: 'var(--r-pill)',
                      background: s.bg,
                      color: s.color,
                      border: `1px solid ${s.border}`,
                    }}
                  >
                    {test.status}
                  </span>
                  {test.status === 'ready' && (
                    <Link to={ROUTES.TEST_RUNNER(test.id)} style={{ textDecoration: 'none' }}>
                      <button
                        className="lx-btn-primary"
                        style={{ padding: '8px 16px', fontSize: 13 }}
                      >
                        Run →
                      </button>
                    </Link>
                  )}
                </div>
              )
            })}
          </div>
        )}
      </div>
    </div>
  )
}
