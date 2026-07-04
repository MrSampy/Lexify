import { AlertTriangle } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Button } from '@/shared/ui'

interface Props {
  message: string
  onRetry: () => void
}

export function ImportErrorBanner({ message, onRetry }: Props) {
  const { t } = useTranslation()
  return (
    <div className="mb-4 rounded-lg border border-destructive/40 bg-destructive/10 p-4">
      <div className="mb-1 flex items-center gap-2">
        <AlertTriangle className="h-4 w-4 text-destructive" />
        <p className="text-sm font-medium text-destructive">{t('import.failedTitle')}</p>
      </div>
      <p className="mb-3 text-xs text-muted-foreground">{message}</p>
      <Button variant="outline" size="sm" onClick={onRetry}>
        {t('common.tryAgain')}
      </Button>
    </div>
  )
}
