import { AlertTriangle } from 'lucide-react'
import { Button } from '@/shared/ui'

interface Props {
  message: string
  onRetry: () => void
}

export function ImportErrorBanner({ message, onRetry }: Props) {
  return (
    <div className="mb-4 rounded-lg border border-destructive/40 bg-destructive/10 p-4">
      <div className="mb-1 flex items-center gap-2">
        <AlertTriangle className="h-4 w-4 text-destructive" />
        <p className="text-sm font-medium text-destructive">AI formatting failed</p>
      </div>
      <p className="mb-3 text-xs text-muted-foreground">{message}</p>
      <Button variant="outline" size="sm" onClick={onRetry}>
        Try again
      </Button>
    </div>
  )
}
