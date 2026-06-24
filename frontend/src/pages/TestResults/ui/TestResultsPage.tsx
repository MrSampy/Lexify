import { Link, useNavigate, useParams } from 'react-router-dom'
import { PieChart, Pie, Cell, Tooltip, ResponsiveContainer } from 'recharts'
import { ROUTES } from '@/shared/config'
import { Button, Spinner } from '@/shared/ui'
import { useAttemptResults } from '@/entities/test'
import { formatPercent } from '@/shared/lib'

export function TestResultsPage() {
  const { id: attemptId } = useParams<{ id: string }>()
  const navigate = useNavigate()

  const { data, isLoading, isError } = useAttemptResults(attemptId ?? '')

  if (isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <Spinner size="lg" />
      </div>
    )
  }

  if (isError || !data) {
    return (
      <div className="flex min-h-screen flex-col items-center justify-center gap-4">
        <p className="text-muted-foreground">Results not available yet.</p>
        <Link to={ROUTES.TESTS} className="text-sm text-primary hover:underline">
          Back to tests
        </Link>
      </div>
    )
  }

  const wrongAnswers = data.answers.filter((a) => !a.isCorrect)
  const chartData = [
    { name: 'Correct', value: data.correctAnswers },
    { name: 'Incorrect', value: data.totalQuestions - data.correctAnswers },
  ]
  const COLORS = ['#22c55e', '#ef4444']

  return (
    <div className="min-h-screen bg-background">
      <div className="mx-auto max-w-2xl px-4 py-8">
        {/* Header */}
        <div className="mb-6">
          <Link
            to={ROUTES.TESTS}
            className="mb-2 inline-block text-sm text-muted-foreground hover:underline"
          >
            ← Tests
          </Link>
          <h1 className="text-2xl font-bold">Test results</h1>
        </div>

        {/* Score card */}
        <div className="mb-6 rounded-lg border bg-card p-6 shadow-sm">
          <div className="flex flex-col items-center gap-6 sm:flex-row">
            {/* Donut chart */}
            <div className="h-40 w-40 shrink-0">
              <ResponsiveContainer width="100%" height="100%">
                <PieChart>
                  <Pie
                    data={chartData}
                    cx="50%"
                    cy="50%"
                    innerRadius={45}
                    outerRadius={65}
                    dataKey="value"
                    strokeWidth={0}
                  >
                    {chartData.map((_, index) => (
                      <Cell key={index} fill={COLORS[index]} />
                    ))}
                  </Pie>
                  <Tooltip />
                </PieChart>
              </ResponsiveContainer>
            </div>

            {/* Score text */}
            <div className="text-center sm:text-left">
              <p className="text-5xl font-bold">{formatPercent(data.score)}</p>
              <p className="mt-1 text-muted-foreground">
                {data.correctAnswers} / {data.totalQuestions} correct
              </p>
              <div className="mt-3 flex gap-4 text-sm">
                <span className="flex items-center gap-1">
                  <span className="h-2.5 w-2.5 rounded-full bg-green-500" />
                  Correct: {data.correctAnswers}
                </span>
                <span className="flex items-center gap-1">
                  <span className="h-2.5 w-2.5 rounded-full bg-red-500" />
                  Wrong: {data.totalQuestions - data.correctAnswers}
                </span>
              </div>
            </div>
          </div>
        </div>

        {/* Wrong answers */}
        {wrongAnswers.length > 0 && (
          <div className="mb-6 rounded-lg border bg-card shadow-sm">
            <div className="border-b px-5 py-3">
              <h2 className="text-sm font-semibold">Incorrect answers ({wrongAnswers.length})</h2>
            </div>
            <div className="divide-y">
              {wrongAnswers.map((a) => (
                <div key={a.questionId} className="px-5 py-3 text-sm">
                  <p className="mb-1 font-medium">{a.questionText}</p>
                  <div className="space-y-0.5 text-xs text-muted-foreground">
                    <p>
                      Your answer: <span className="text-destructive">{a.givenAnswer || '—'}</span>
                    </p>
                    <p>
                      Correct: <span className="font-medium text-green-600">{a.correctAnswer}</span>
                    </p>
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* Actions */}
        <div className="flex flex-wrap gap-3">
          <Button onClick={() => navigate(ROUTES.TEST_RUNNER(data.testId))}>Try again</Button>
          <Button variant="outline" onClick={() => navigate(ROUTES.TESTS)}>
            Back to tests
          </Button>
        </div>
      </div>
    </div>
  )
}
