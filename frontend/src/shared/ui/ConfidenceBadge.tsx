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
      className={className}
      title="Low confidence"
      style={{
        display: 'inline-flex',
        alignItems: 'center',
        gap: 4,
        color: 'var(--warning)',
      }}
    >
      <AlertTriangle style={{ width: 13, height: 13 }} />
      {showLabel && (
        <span style={{ fontFamily: 'var(--font-mono)', fontSize: 10, fontWeight: 600 }}>
          review
        </span>
      )}
    </span>
  )
}
