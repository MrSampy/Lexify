import { AlertTriangle } from 'lucide-react'

interface ConfidenceBadgeProps {
  flag: boolean
  showLabel?: boolean
  className?: string
}

export function ConfidenceBadge({ flag, showLabel = false, className }: ConfidenceBadgeProps) {
  if (!flag) return null

  return (
    <span
      className={`inline-flex items-center gap-1 text-[var(--warning)] ${className ?? ''}`}
      title="Low confidence"
    >
      <AlertTriangle className="h-[13px] w-[13px]" />
      {showLabel && (
        <span className="text-[10px] font-bold [font-family:var(--font-body)]">review</span>
      )}
    </span>
  )
}
