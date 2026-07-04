import { Sparkles } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Input } from '@/shared/ui'
import { useImportWordsStore } from '../model/store'

export function BlockTitleInput() {
  const { t } = useTranslation()
  const suggestedTitle = useImportWordsStore((s) => s.suggestedTitle)
  const setSuggestedTitle = useImportWordsStore((s) => s.setSuggestedTitle)

  return (
    <div>
      <label className="mb-1 flex items-center gap-1.5 text-sm font-medium">
        <Sparkles className="h-3.5 w-3.5 text-amber-500" />
        {t('import.blockTitle')}
      </label>
      <Input
        value={suggestedTitle}
        onChange={(e) => setSuggestedTitle(e.target.value)}
        placeholder={t('import.blockTitlePlaceholder')}
      />
      <p className="mt-1 text-xs text-muted-foreground">{t('import.aiSuggested')}</p>
    </div>
  )
}
