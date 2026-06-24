import { AlertTriangle } from 'lucide-react'
import { cn } from '@/lib/utils'

interface ConfidenceBadgeProps {
  flag: boolean
  showLabel?: boolean
  className?: string
}

export function ConfidenceBadge({ flag, showLabel = false, className }: ConfidenceBadgeProps) {
  if (!flag) return null

  return (
    <span
      className={cn('inline-flex items-center gap-1 text-amber-500', className)}
      title="Low confidence"
    >
      <AlertTriangle className="h-4 w-4" />
      {showLabel && <span className="text-xs font-medium">Review</span>}
    </span>
  )
}
