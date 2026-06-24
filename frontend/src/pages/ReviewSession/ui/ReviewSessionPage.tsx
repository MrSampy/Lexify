import { useState } from 'react'
import { Link } from 'react-router-dom'
import { ROUTES } from '@/shared/config'
import { Button, Spinner } from '@/shared/ui'
import { useDueWords, useRateWordMutation, ReviewCard, QualityRater } from '@/features/review-word'
import { TestProgressBar } from '@/features/run-test'

interface RatingEntry {
  wordId: string
  quality: number
}

export function ReviewSessionPage() {
  const [currentIndex, setCurrentIndex] = useState(0)
  const [ratings, setRatings] = useState<RatingEntry[]>([])
  const [isFinished, setIsFinished] = useState(false)

  const { data: words, isLoading, isError } = useDueWords()
  const rateWord = useRateWordMutation()

  const handleRate = (quality: number) => {
    const word = words![currentIndex]
    rateWord.mutate({ wordId: word.id, quality })
    const newRatings = [...ratings, { wordId: word.id, quality }]
    setRatings(newRatings)

    if (currentIndex + 1 === words!.length) {
      setIsFinished(true)
    } else {
      setCurrentIndex((i) => i + 1)
    }
  }

  if (isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <Spinner size="lg" />
      </div>
    )
  }

  if (isError) {
    return (
      <div className="flex min-h-screen flex-col items-center justify-center gap-4">
        <p className="text-muted-foreground">Не вдалося завантажити слова для повторення.</p>
        <Link to={ROUTES.DASHBOARD} className="text-sm text-primary hover:underline">
          На головну
        </Link>
      </div>
    )
  }

  if (!words || words.length === 0) {
    return (
      <div className="flex min-h-screen flex-col items-center justify-center gap-4">
        <p className="text-2xl">🎉</p>
        <p className="text-lg font-medium">Немає слів для повторення!</p>
        <p className="text-sm text-muted-foreground">
          Повертайтесь пізніше або додайте нові слова.
        </p>
        <Link to={ROUTES.BLOCKS}>
          <Button variant="outline">До блоків</Button>
        </Link>
      </div>
    )
  }

  if (isFinished) {
    const hardCount = ratings.filter((r) => r.quality < 3).length
    const easyCount = ratings.filter((r) => r.quality >= 3).length
    return (
      <div className="flex min-h-screen flex-col items-center justify-center gap-6 px-4">
        <p className="text-4xl">✅</p>
        <div className="text-center">
          <h1 className="text-2xl font-bold">Сесія завершена!</h1>
          <p className="mt-1 text-muted-foreground">Пройдено: {ratings.length} слів</p>
        </div>
        <div className="flex gap-8 text-center">
          <div>
            <p className="text-3xl font-bold text-red-500">{hardCount}</p>
            <p className="text-sm text-muted-foreground">Складних (0–2)</p>
          </div>
          <div>
            <p className="text-3xl font-bold text-green-500">{easyCount}</p>
            <p className="text-sm text-muted-foreground">Легких (3–5)</p>
          </div>
        </div>
        <Link to={ROUTES.DASHBOARD}>
          <Button>До дашборду</Button>
        </Link>
      </div>
    )
  }

  const currentWord = words[currentIndex]

  return (
    <div className="min-h-screen bg-background">
      <div className="mx-auto max-w-xl px-4 py-8">
        {/* Header */}
        <div className="mb-6">
          <Link
            to={ROUTES.DASHBOARD}
            className="mb-2 inline-block text-sm text-muted-foreground hover:underline"
          >
            ← Дашборд
          </Link>
          <h1 className="text-xl font-bold">Повторення слів</h1>
        </div>

        {/* Progress */}
        <div className="mb-6">
          <TestProgressBar
            current={currentIndex + 1}
            total={words.length}
            correctCount={ratings.filter((r) => r.quality >= 3).length}
          />
        </div>

        {/* Card */}
        <div className="mb-6">
          <ReviewCard key={currentWord.id} word={currentWord} />
        </div>

        {/* Rating */}
        <div>
          <p className="mb-3 text-center text-sm text-muted-foreground">
            Як добре ти пам'ятав це слово?
          </p>
          <QualityRater onRate={handleRate} disabled={rateWord.isPending} />
        </div>
      </div>
    </div>
  )
}
