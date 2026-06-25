import { Link } from 'react-router-dom'
import { ROUTES, LANGUAGES } from '@/shared/config'
import { Spinner, LanguageBadge } from '@/shared/ui'
import { useAuthStore, useUserStats } from '@/entities/user'
import { useBlocks } from '@/entities/block'
import { useTests } from '@/entities/test'
import { ReviewDueBanner } from '@/widgets/ReviewDueBanner'

function StatCard({ label, value }: { label: string; value: number | undefined }) {
  return (
    <div className="rounded-lg border bg-card p-4 text-center shadow-sm">
      <p className="text-2xl font-bold">{value ?? '—'}</p>
      <p className="mt-1 text-xs text-muted-foreground">{label}</p>
    </div>
  )
}

export function DashboardPage() {
  const user = useAuthStore((s) => s.user)
  const { data: stats } = useUserStats()
  const { data: blocksPage, isLoading: blocksLoading } = useBlocks({ page: 1, pageSize: 3 })
  const { data: testsPage, isLoading: testsLoading } = useTests(undefined, 1)

  const recentBlocks = blocksPage?.items ?? []
  const recentTests = (testsPage?.items ?? []).slice(0, 3)

  return (
    <div className="min-h-screen bg-background">
      <div className="mx-auto max-w-3xl px-4 py-8">
        {/* Greeting */}
        <div className="mb-8">
          <h1 className="text-2xl font-bold">
            Привіт{user?.email ? `, ${user.email.split('@')[0]}` : ''}! 👋
          </h1>
          <p className="mt-1 text-muted-foreground">Що будемо вчити сьогодні?</p>
        </div>

        {/* Review banner */}
        <div className="mb-8">
          <ReviewDueBanner />
        </div>

        {/* Quick navigation */}
        <div className="mb-8 grid grid-cols-2 gap-4">
          <Link to={ROUTES.BLOCKS}>
            <div className="rounded-lg border bg-card p-5 shadow-sm transition-shadow hover:shadow-md">
              <p className="text-lg font-semibold">📚 Блоки</p>
              <p className="mt-1 text-sm text-muted-foreground">Переглянути та редагувати слова</p>
            </div>
          </Link>
          <Link to={ROUTES.TESTS}>
            <div className="rounded-lg border bg-card p-5 shadow-sm transition-shadow hover:shadow-md">
              <p className="text-lg font-semibold">🧪 Тести</p>
              <p className="mt-1 text-sm text-muted-foreground">Пройти або створити тест</p>
            </div>
          </Link>
        </div>

        {/* Weekly stats */}
        <div className="mb-8">
          <h2 className="mb-3 text-sm font-semibold uppercase tracking-wide text-muted-foreground">
            Статистика
          </h2>
          <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
            <StatCard label="Блоків" value={stats?.totalBlocks} />
            <StatCard label="Слів усього" value={stats?.totalWords} />
            <StatCard label="Відповідей цього тижня" value={stats?.wordsAnsweredThisWeek} />
            <StatCard label="Тестів цього тижня" value={stats?.testsCompletedThisWeek} />
          </div>
        </div>

        {/* Recent blocks */}
        <div className="mb-8">
          <div className="mb-3 flex items-center justify-between">
            <h2 className="text-sm font-semibold uppercase tracking-wide text-muted-foreground">
              Останні блоки
            </h2>
            <Link to={ROUTES.BLOCKS} className="text-xs text-primary hover:underline">
              Всі блоки →
            </Link>
          </div>
          {blocksLoading ? (
            <div className="flex justify-center py-6">
              <Spinner />
            </div>
          ) : recentBlocks.length === 0 ? (
            <p className="text-sm text-muted-foreground">Ще немає блоків. Створи перший!</p>
          ) : (
            <div className="divide-y rounded-lg border bg-card">
              {recentBlocks.map((block) => {
                const langCode = LANGUAGES[block.languageId]?.code ?? String(block.languageId)
                return (
                  <Link
                    key={block.id}
                    to={ROUTES.BLOCK_DETAIL(block.id)}
                    className="flex items-center justify-between px-4 py-3 hover:bg-accent"
                  >
                    <div className="flex items-center gap-2">
                      <span className="font-medium">{block.title}</span>
                      <LanguageBadge code={langCode} />
                    </div>
                    <span className="text-xs text-muted-foreground">
                      {block.wordCount} {block.wordCount === 1 ? 'слово' : 'слів'}
                    </span>
                  </Link>
                )
              })}
            </div>
          )}
        </div>

        {/* Recent tests */}
        <div className="mb-8">
          <div className="mb-3 flex items-center justify-between">
            <h2 className="text-sm font-semibold uppercase tracking-wide text-muted-foreground">
              Останні тести
            </h2>
            <Link to={ROUTES.TESTS} className="text-xs text-primary hover:underline">
              Всі тести →
            </Link>
          </div>
          {testsLoading ? (
            <div className="flex justify-center py-6">
              <Spinner />
            </div>
          ) : recentTests.length === 0 ? (
            <p className="text-sm text-muted-foreground">Ще немає тестів. Створи перший!</p>
          ) : (
            <div className="divide-y rounded-lg border bg-card">
              {recentTests.map((test) => (
                <Link
                  key={test.id}
                  to={test.status === 'ready' ? ROUTES.TEST_RUNNER(test.id) : ROUTES.TESTS}
                  className="flex items-center justify-between px-4 py-3 hover:bg-accent"
                >
                  <span className="font-medium">{test.title}</span>
                  <span
                    className={`rounded-full px-2 py-0.5 text-xs font-medium ${
                      test.status === 'ready'
                        ? 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-400'
                        : test.status === 'generating'
                          ? 'bg-amber-100 text-amber-800 dark:bg-amber-900/30 dark:text-amber-400'
                          : 'bg-muted text-muted-foreground'
                    }`}
                  >
                    {test.status === 'ready'
                      ? 'Готовий'
                      : test.status === 'generating'
                        ? 'Генерується'
                        : 'Архів'}
                  </span>
                </Link>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  )
}
