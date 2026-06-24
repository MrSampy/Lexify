interface QualityRaterProps {
  onRate: (quality: number) => void
  disabled: boolean
}

const QUALITY_BUTTONS = [
  {
    quality: 0,
    label: 'Забув',
    className: 'border-red-400 text-red-600 hover:bg-red-50 dark:hover:bg-red-950/30',
  },
  {
    quality: 1,
    label: 'Складно',
    className: 'border-orange-400 text-orange-600 hover:bg-orange-50 dark:hover:bg-orange-950/30',
  },
  {
    quality: 2,
    label: 'Нормально',
    className: 'border-yellow-400 text-yellow-600 hover:bg-yellow-50 dark:hover:bg-yellow-950/30',
  },
  {
    quality: 3,
    label: 'Легко',
    className: 'border-lime-400 text-lime-700 hover:bg-lime-50 dark:hover:bg-lime-950/30',
  },
  {
    quality: 4,
    label: 'Відмінно',
    className: 'border-green-500 text-green-700 hover:bg-green-50 dark:hover:bg-green-950/30',
  },
  {
    quality: 5,
    label: 'Ідеально',
    className:
      'border-emerald-500 text-emerald-700 hover:bg-emerald-50 dark:hover:bg-emerald-950/30',
  },
]

export function QualityRater({ onRate, disabled }: QualityRaterProps) {
  return (
    <div className="grid grid-cols-3 gap-2 sm:grid-cols-6">
      {QUALITY_BUTTONS.map(({ quality, label, className }) => (
        <button
          key={quality}
          onClick={() => onRate(quality)}
          disabled={disabled}
          className={`rounded-lg border-2 px-3 py-3 text-sm font-medium transition-colors disabled:cursor-not-allowed disabled:opacity-50 ${className}`}
        >
          <span className="block text-xs text-muted-foreground">{quality}</span>
          {label}
        </button>
      ))}
    </div>
  )
}
