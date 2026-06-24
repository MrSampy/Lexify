import { cn } from '@/lib/utils'

const COLOR_MAP: Record<string, string> = {
  word: 'bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-300',
  phrase: 'bg-purple-100 text-purple-700 dark:bg-purple-900 dark:text-purple-300',
  idiom: 'bg-amber-100 text-amber-700 dark:bg-amber-900 dark:text-amber-300',
  expression: 'bg-green-100 text-green-700 dark:bg-green-900 dark:text-green-300',
}

interface WordTypeBadgeProps {
  type: string
  className?: string
}

export function WordTypeBadge({ type, className }: WordTypeBadgeProps) {
  const colors = COLOR_MAP[type.toLowerCase()] ?? 'bg-muted text-muted-foreground'
  return (
    <span
      className={cn(
        'inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium capitalize',
        colors,
        className,
      )}
    >
      {type}
    </span>
  )
}
