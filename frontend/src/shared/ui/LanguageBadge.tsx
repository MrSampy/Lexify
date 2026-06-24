import { cn } from '@/lib/utils'

interface LanguageBadgeProps {
  code: string
  className?: string
}

export function LanguageBadge({ code, className }: LanguageBadgeProps) {
  return (
    <span
      className={cn(
        'inline-flex items-center rounded-full border px-2 py-0.5 text-xs font-semibold uppercase tracking-wide',
        className,
      )}
    >
      {code}
    </span>
  )
}
