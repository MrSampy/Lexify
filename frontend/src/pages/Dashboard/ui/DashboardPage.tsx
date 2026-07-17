import { Link, useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Flame, Library, Type, CheckCircle2, FileText, ArrowRight, RotateCcw } from 'lucide-react'
import { ROUTES, LANGUAGES } from '@/shared/config'
import { Mascot, Spinner } from '@/shared/ui'
import { useAuthStore, useProfile, useUserStats } from '@/entities/user'
import { useBlocks } from '@/entities/block'
import { useTests } from '@/entities/test'

/** One slim inline figure in the stats strip — icon, number, caption. */
function StatItem({
  icon,
  label,
  value,
}: {
  icon: React.ReactNode
  label: string
  value: number | undefined
}) {
  return (
    <div style={{ display: 'flex', alignItems: 'center', gap: 10, minWidth: 0 }}>
      <span
        style={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          width: 34,
          height: 34,
          flexShrink: 0,
          borderRadius: 'var(--r-sm)',
          background: 'var(--accent-ghost)',
          color: 'var(--accent-dim)',
        }}
      >
        {icon}
      </span>
      <div style={{ minWidth: 0 }}>
        {value === undefined ? (
          <div
            className="animate-pulse"
            style={{
              width: 34,
              height: 20,
              borderRadius: 4,
              background: 'var(--bg-3)',
              marginBottom: 2,
            }}
          />
        ) : (
          <div
            style={{
              fontSize: 20,
              lineHeight: 1.1,
              fontWeight: 800,
              color: 'var(--fg-1)',
              fontFamily: 'var(--font-display)',
            }}
          >
            {value}
          </div>
        )}
        <div
          className="ds-sm"
          style={{
            color: 'var(--fg-3)',
            fontWeight: 600,
            whiteSpace: 'nowrap',
            overflow: 'hidden',
            textOverflow: 'ellipsis',
          }}
        >
          {label}
        </div>
      </div>
    </div>
  )
}

const STATUS_STYLES: Record<string, { bg: string; color: string; border: string }> = {
  ready: { bg: 'var(--success-ghost)', color: 'var(--success)', border: 'var(--accent-line)' },
  generating: {
    bg: 'var(--warning-ghost)',
    color: 'var(--warning)',
    border: 'var(--warning-ghost)',
  },
  archived: { bg: 'var(--bg-3)', color: 'var(--fg-3)', border: 'var(--line-2)' },
}

export function DashboardPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const user = useAuthStore((s) => s.user)
  const { data: profile } = useProfile()
  const { data: stats, isError: statsError } = useUserStats()
  const { data: blocksPage, isLoading: blocksLoading } = useBlocks({ page: 1, pageSize: 3 })
  const { data: testsPage, isLoading: testsLoading } = useTests(undefined, 1)

  const recentBlocks = blocksPage?.items ?? []
  const recentTests = (testsPage?.items ?? []).slice(0, 3)
  // The JWT carries no displayName claim (auth-store user always has displayName: null), so the
  // real name comes from the profile endpoint; email localpart is only the fallback while it loads
  // or when the user never set a name.
  const displayName =
    profile?.displayName ?? user?.email?.split('@')[0] ?? t('dashboard.fallbackName')

  const due = stats?.dueWordsCount ?? 0
  const streak = stats?.currentStreak ?? 0

  return (
    <div>
      {/* ── Hero: greeting + the ONE primary action (review) ─────────────────── */}
      <div
        className="lx-card"
        style={{
          display: 'flex',
          alignItems: 'center',
          flexWrap: 'wrap',
          gap: 20,
          padding: 'clamp(20px, 4vw, 28px)',
          marginBottom: 16,
          background: 'linear-gradient(135deg, var(--accent-ghost) 0%, var(--bg-2) 55%)',
          border: '1px solid var(--accent-line)',
        }}
      >
        <div style={{ flex: 1, minWidth: 220 }}>
          <h1 className="ds-h2" style={{ margin: '0 0 4px' }}>
            {t('dashboard.greeting', { name: displayName })}
          </h1>
          <p className="ds-body" style={{ margin: 0, color: 'var(--fg-3)' }}>
            {due > 0 ? t('dashboard.reviewDue', { count: due }) : t('dashboard.noDue')}
            {due > 0 && stats && (
              <span style={{ color: 'var(--fg-4)' }}>
                {' '}
                ·{' '}
                {t('review.queueComposition', {
                  newCount: stats.dueNewCount,
                  reviewCount: stats.dueReviewCount,
                })}
              </span>
            )}
          </p>
        </div>

        <div style={{ display: 'flex', alignItems: 'center', gap: 14, flexWrap: 'wrap' }}>
          <Mascot pose={due > 0 ? 'pointing' : 'greeting'} size={84} float />
          {streak > 0 && (
            <span
              title={t('dashboard.statStreak')}
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: 6,
                padding: '7px 14px',
                borderRadius: 'var(--r-pill)',
                background: 'var(--bg-1)',
                border: '1px solid var(--accent-line)',
                color: 'var(--fg-2)',
                fontWeight: 800,
                fontSize: 14,
                fontFamily: 'var(--font-display)',
              }}
            >
              <Flame style={{ width: 16, height: 16, color: 'var(--warning)' }} />
              {streak} {t('dashboard.statStreak')}
            </span>
          )}
          {due > 0 ? (
            <button
              className="lx-btn-primary"
              style={{ padding: '12px 24px', fontSize: 15 }}
              onClick={() => navigate(ROUTES.REVIEW)}
            >
              {t('dashboard.startReview')}
            </button>
          ) : (
            <button
              className="lx-btn-secondary"
              style={{ padding: '12px 24px', fontSize: 15 }}
              onClick={() => navigate(`${ROUTES.REVIEW}?mode=cram`)}
            >
              <RotateCcw style={{ width: 15, height: 15, marginRight: 8 }} />
              {t('dashboard.practiceAnyway')}
            </button>
          )}
        </div>
      </div>

      {/* ── Stats strip: one quiet row instead of six cards ──────────────────── */}
      <div
        className="lx-card"
        style={{
          display: 'flex',
          alignItems: 'center',
          flexWrap: 'wrap',
          gap: 'clamp(16px, 3vw, 32px)',
          padding: '14px 20px',
          marginBottom: 36,
        }}
      >
        {statsError && (
          <span className="ds-sm" style={{ color: 'var(--danger)', fontWeight: 600 }}>
            {t('dashboard.statsFailed')}
          </span>
        )}
        <StatItem
          icon={<Library style={{ width: 17, height: 17 }} />}
          label={t('dashboard.statBlocks')}
          value={stats?.totalBlocks}
        />
        <StatItem
          icon={<Type style={{ width: 17, height: 17 }} />}
          label={t('dashboard.statWords')}
          value={stats?.totalWords}
        />
        <StatItem
          icon={<CheckCircle2 style={{ width: 17, height: 17 }} />}
          label={t('dashboard.statAnswers')}
          value={stats?.wordsAnsweredThisWeek}
        />
        <StatItem
          icon={<FileText style={{ width: 17, height: 17 }} />}
          label={t('dashboard.statTests')}
          value={stats?.testsCompletedThisWeek}
        />
        <div style={{ flex: 1 }} />
        <Link
          to={ROUTES.STATS}
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: 6,
            color: 'var(--accent-color)',
            fontSize: 13,
            fontWeight: 700,
            textDecoration: 'none',
            whiteSpace: 'nowrap',
          }}
        >
          {t('dashboard.viewStats')}
          <ArrowRight style={{ width: 14, height: 14 }} />
        </Link>
      </div>

      {/* ── Recent blocks + recent tests, side by side on desktop ────────────── */}
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fit, minmax(320px, 1fr))',
          gap: 'clamp(20px, 3vw, 32px)',
          alignItems: 'start',
        }}
      >
        {/* Recent blocks */}
        <div>
          <div className="lx-section-head">
            <h2 className="ds-h4 m-0">{t('dashboard.recentBlocks')}</h2>
            <div className="lx-rule" />
            <Link
              to={ROUTES.BLOCKS}
              style={{
                color: 'var(--accent-color)',
                fontSize: 13,
                fontWeight: 700,
                flexShrink: 0,
                textDecoration: 'none',
              }}
            >
              {t('dashboard.allBlocks')}
            </Link>
          </div>

          {blocksLoading ? (
            <div style={{ display: 'flex', justifyContent: 'center', padding: '24px 0' }}>
              <Spinner />
            </div>
          ) : recentBlocks.length === 0 ? (
            <p className="ds-sm" style={{ color: 'var(--fg-3)' }}>
              {t('dashboard.noBlocks')}{' '}
              <Link to={ROUTES.BLOCKS} style={{ color: 'var(--accent-color)', fontWeight: 700 }}>
                {t('dashboard.createFirstBlock')}
              </Link>
            </p>
          ) : (
            <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
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
                      style={{
                        display: 'flex',
                        alignItems: 'center',
                        gap: 12,
                        padding: '14px 18px',
                        cursor: 'pointer',
                      }}
                    >
                      <div style={{ flex: 1, minWidth: 0 }}>
                        <div
                          className="ds-h4"
                          style={{
                            color: 'var(--fg-1)',
                            fontSize: 15,
                            overflow: 'hidden',
                            textOverflow: 'ellipsis',
                            whiteSpace: 'nowrap',
                          }}
                        >
                          {block.title}
                        </div>
                        <div className="ds-sm" style={{ color: 'var(--fg-4)', marginTop: 2 }}>
                          {t('blocks.wordCount', { count: block.wordCount })}
                        </div>
                      </div>
                      <span
                        style={{
                          fontSize: 11,
                          padding: '3px 10px',
                          borderRadius: 'var(--r-pill)',
                          background: 'var(--accent-ghost)',
                          color: 'var(--accent-dim)',
                          fontWeight: 700,
                          flexShrink: 0,
                          border: '1px solid var(--accent-line)',
                        }}
                      >
                        {langCode.toUpperCase()}
                      </span>
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
            <h2 className="ds-h4 m-0">{t('dashboard.recentTests')}</h2>
            <div className="lx-rule" />
            <Link
              to={ROUTES.TESTS}
              style={{
                color: 'var(--accent-color)',
                fontSize: 13,
                fontWeight: 700,
                flexShrink: 0,
                textDecoration: 'none',
              }}
            >
              {t('dashboard.allTests')}
            </Link>
          </div>

          {testsLoading ? (
            <div style={{ display: 'flex', justifyContent: 'center', padding: '24px 0' }}>
              <Spinner />
            </div>
          ) : recentTests.length === 0 ? (
            <p className="ds-sm" style={{ color: 'var(--fg-3)' }}>
              {t('dashboard.noTests')}{' '}
              <Link
                to={ROUTES.TEST_CREATE}
                style={{ color: 'var(--accent-color)', fontWeight: 700 }}
              >
                {t('dashboard.createFirstTest')}
              </Link>
            </p>
          ) : (
            <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
              {recentTests.map((test) => {
                const s = STATUS_STYLES[test.status] ?? STATUS_STYLES.archived
                return (
                  <div
                    key={test.id}
                    className="lx-card"
                    style={{
                      display: 'flex',
                      alignItems: 'center',
                      gap: 12,
                      padding: '14px 18px',
                      flexWrap: 'wrap',
                    }}
                  >
                    <div style={{ flex: 1, minWidth: 140 }}>
                      <div
                        className="ds-h4"
                        style={{
                          color: 'var(--fg-1)',
                          fontSize: 15,
                          overflow: 'hidden',
                          textOverflow: 'ellipsis',
                          whiteSpace: 'nowrap',
                        }}
                      >
                        {test.title}
                      </div>
                    </div>
                    <span
                      style={{
                        fontSize: 11,
                        padding: '4px 12px',
                        borderRadius: 'var(--r-pill)',
                        background: s.bg,
                        color: s.color,
                        border: `1px solid ${s.border}`,
                        fontWeight: 700,
                        flexShrink: 0,
                      }}
                    >
                      {t(`tests.status.${test.status}`, { defaultValue: test.status })}
                    </span>
                    {test.status === 'ready' && (
                      <Link to={ROUTES.TEST_RUNNER(test.id)} style={{ textDecoration: 'none' }}>
                        <button
                          className="lx-btn-primary"
                          style={{ padding: '7px 16px', fontSize: 13 }}
                        >
                          {t('dashboard.run')}
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
    </div>
  )
}
