import { Link } from 'react-router-dom'
import { ROUTES } from '@/shared/config'
import { useAuthStore } from '@/entities/user'
import { ReviewDueBanner } from '@/widgets/ReviewDueBanner'

export function DashboardPage() {
  const user = useAuthStore((s) => s.user)

  return (
    <div className="min-h-screen bg-background">
      <div className="mx-auto max-w-2xl px-4 py-8">
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
        <div className="grid grid-cols-2 gap-4">
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
      </div>
    </div>
  )
}
