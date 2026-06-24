interface TestProgressBarProps {
  current: number
  total: number
  correctCount: number
}

export function TestProgressBar({ current, total, correctCount }: TestProgressBarProps) {
  const percent = Math.round((current / total) * 100)
  return (
    <div>
      <div className="mb-1 flex items-center justify-between text-xs text-muted-foreground">
        <span>
          Question {current} of {total}
        </span>
        <span className="text-green-600">{correctCount} correct</span>
      </div>
      <div className="h-1.5 w-full overflow-hidden rounded-full bg-muted">
        <div
          className="h-full rounded-full bg-primary transition-all duration-300"
          style={{ width: `${percent}%` }}
        />
      </div>
    </div>
  )
}
