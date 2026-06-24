import { useNavigate } from 'react-router-dom'
import { ROUTES } from '@/shared/config'
import { Button } from '@/shared/ui'
import { useDueWords } from '@/features/review-word'

export function ReviewDueBanner() {
  const navigate = useNavigate()
  const { data, isLoading } = useDueWords()

  if (isLoading || !data || data.length === 0) return null

  return (
    <div className="flex items-center justify-between rounded-lg border border-amber-300 bg-amber-50 px-5 py-4 dark:border-amber-700 dark:bg-amber-950/30">
      <div>
        <p className="font-semibold text-amber-900 dark:text-amber-200">
          Сьогодні до повторення: <span className="text-xl">{data.length}</span>{' '}
          {data.length === 1 ? 'слово' : data.length < 5 ? 'слова' : 'слів'}
        </p>
        <p className="mt-0.5 text-sm text-amber-700 dark:text-amber-400">
          Займе приблизно {Math.ceil(data.length * 0.5)} хв
        </p>
      </div>
      <Button
        onClick={() => navigate(ROUTES.REVIEW)}
        className="bg-amber-600 text-white hover:bg-amber-700 dark:bg-amber-600 dark:hover:bg-amber-500"
      >
        Почати
      </Button>
    </div>
  )
}
