import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { ROUTES, LANGUAGES } from '@/shared/config'
import { Spinner } from '@/shared/ui'
import { useAuthStore, useUserStats } from '@/entities/user'
import { useBlocks } from '@/entities/block'
import { useTests } from '@/entities/test'
import { ReviewDueBanner } from '@/widgets/ReviewDueBanner'

function StatCard({
  label,
  value,
  emoji,
}: {
  label: string
  value: number | undefined
  emoji: string
}) {
  return (
    <div className="lx-card px-6 py-5 text-center">
      <div className="mb-1.5 text-[28px]">{emoji}</div>
      <div className="text-[30px] leading-none font-extrabold text-[var(--fg-1)] [font-family:var(--font-display)]">
        {value ?? '—'}
      </div>
      <div className="ds-sm mt-1 font-semibold text-[var(--fg-3)]">{label}</div>
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
  const user = useAuthStore((s) => s.user)
  const { data: stats } = useUserStats()
  const { data: blocksPage, isLoading: blocksLoading } = useBlocks({ page: 1, pageSize: 3 })
  const { data: testsPage, isLoading: testsLoading } = useTests(undefined, 1)

  const recentBlocks = blocksPage?.items ?? []
  const recentTests = (testsPage?.items ?? []).slice(0, 3)
  const displayName = user?.displayName ?? user?.email?.split('@')[0] ?? t('dashboard.fallbackName')

  return (
    <div>
      {/* Greeting */}
      <h1 className="ds-h2" style={{ margin: '0 0 6px' }}>
        {t('dashboard.greeting', { name: displayName })}
      </h1>
      <p className="ds-body" style={{ margin: '0 0 24px', color: 'var(--fg-3)' }}>
        {t('dashboard.subtitle')}
      </p>

      {/* Review banner */}
      <div style={{ marginBottom: 26 }}>
        <ReviewDueBanner />
      </div>

      {/* Stats */}
      <div className="mb-9 grid grid-cols-[repeat(auto-fit,minmax(150px,1fr))] gap-3.5">
        <StatCard emoji="📚" label={t('dashboard.statBlocks')} value={stats?.totalBlocks} />
        <StatCard emoji="🔤" label={t('dashboard.statWords')} value={stats?.totalWords} />
        <StatCard
          emoji="✅"
          label={t('dashboard.statAnswers')}
          value={stats?.wordsAnsweredThisWeek}
        />
        <StatCard
          emoji="📝"
          label={t('dashboard.statTests')}
          value={stats?.testsCompletedThisWeek}
        />
      </div>

      {/* CTA cards */}
      <div className="mb-9 grid grid-cols-[repeat(auto-fit,minmax(240px,1fr))] gap-4">
        <Link to={ROUTES.BLOCKS} style={{ textDecoration: 'none' }}>
          <div className="lx-card" style={{ padding: 24, cursor: 'pointer' }}>
            <div style={{ fontSize: 32, marginBottom: 12 }}>📚</div>
            <div className="ds-h3" style={{ marginBottom: 6 }}>
              {t('nav.blocks')}
            </div>
            <p className="ds-sm" style={{ color: 'var(--fg-3)', margin: 0 }}>
              {t('dashboard.ctaBlocksDesc')}
            </p>
          </div>
        </Link>

        <Link to={ROUTES.TESTS} style={{ textDecoration: 'none' }}>
          <div className="lx-card" style={{ padding: 24, cursor: 'pointer' }}>
            <div style={{ fontSize: 32, marginBottom: 12 }}>📝</div>
            <div className="ds-h3" style={{ marginBottom: 6 }}>
              {t('nav.tests')}
            </div>
            <p className="ds-sm" style={{ color: 'var(--fg-3)', margin: 0 }}>
              {t('dashboard.ctaTestsDesc')}
            </p>
          </div>
        </Link>
      </div>

      {/* Recent blocks */}
      <div style={{ marginBottom: 32 }}>
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
                  <div className="lx-card" style={{ padding: 18, cursor: 'pointer' }}>
                    <div
                      style={{
                        display: 'flex',
                        justifyContent: 'space-between',
                        alignItems: 'start',
                        gap: 10,
                        marginBottom: 10,
                      }}
                    >
                      <div className="ds-h4" style={{ color: 'var(--fg-1)', fontSize: 15 }}>
                        {block.title}
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
                    <div className="ds-sm" style={{ color: 'var(--fg-4)' }}>
                      {t('blocks.wordCount', { count: block.wordCount })}
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
            <Link to={ROUTES.TEST_CREATE} style={{ color: 'var(--accent-color)', fontWeight: 700 }}>
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
                    gap: 18,
                    padding: '16px 20px',
                    flexWrap: 'wrap',
                  }}
                >
                  <div style={{ flex: 1, minWidth: 180 }}>
                    <div className="ds-h4" style={{ color: 'var(--fg-1)', fontSize: 15 }}>
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
                    }}
                  >
                    {t(`tests.status.${test.status}`, { defaultValue: test.status })}
                  </span>
                  {test.status === 'ready' && (
                    <Link to={ROUTES.TEST_RUNNER(test.id)} style={{ textDecoration: 'none' }}>
                      <button
                        className="lx-btn-primary"
                        style={{ padding: '8px 18px', fontSize: 13 }}
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
  )
}
