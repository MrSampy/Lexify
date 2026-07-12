import { useTranslation } from 'react-i18next'
import { LANGUAGES } from '@/shared/config'
import {
  Button,
  Textarea,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/shared/ui'
import { useImportWordsStore } from '../model/store'
import { validateImportInput } from '../lib/validateImportInput'
import { ImportHelpDialog } from './ImportHelpDialog'

interface Props {
  onSubmit: () => void
}

export function RawTextInput({ onSubmit }: Props) {
  const { t } = useTranslation()
  const rawText = useImportWordsStore((s) => s.rawText)
  const targetLanguageId = useImportWordsStore((s) => s.targetLanguageId)
  const nativeLanguageId = useImportWordsStore((s) => s.nativeLanguageId)
  const setRawText = useImportWordsStore((s) => s.setRawText)
  const setTargetLanguageId = useImportWordsStore((s) => s.setTargetLanguageId)
  const setNativeLanguageId = useImportWordsStore((s) => s.setNativeLanguageId)

  const validationError = validateImportInput(rawText, targetLanguageId, nativeLanguageId)

  return (
    <div className="space-y-4">
      <div className="grid grid-cols-2 gap-3">
        <div>
          <label className="mb-1 block text-sm font-medium">{t('import.targetLanguage')}</label>
          <Select
            value={String(targetLanguageId)}
            onValueChange={(v) => v && setTargetLanguageId(Number(v))}
          >
            <SelectTrigger>
              <SelectValue>{LANGUAGES[targetLanguageId]?.name}</SelectValue>
            </SelectTrigger>
            <SelectContent>
              {Object.entries(LANGUAGES).map(([id, lang]) => (
                <SelectItem key={id} value={id}>
                  {lang.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">{t('import.nativeLanguage')}</label>
          <Select
            value={String(nativeLanguageId)}
            onValueChange={(v) => v && setNativeLanguageId(Number(v))}
          >
            <SelectTrigger>
              <SelectValue>{LANGUAGES[nativeLanguageId]?.name}</SelectValue>
            </SelectTrigger>
            <SelectContent>
              {Object.entries(LANGUAGES).map(([id, lang]) => (
                <SelectItem key={id} value={id}>
                  {lang.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      </div>

      <div>
        <div className="mb-1 flex items-center justify-between">
          <label className="block text-sm font-medium">{t('import.vocabText')}</label>
          <ImportHelpDialog />
        </div>
        <Textarea
          value={rawText}
          onChange={(e) => setRawText(e.target.value)}
          placeholder={t('import.vocabPlaceholder')}
          className="min-h-[220px] resize-y font-mono text-sm"
          maxLength={10_000}
        />
        <p className="mt-1 text-xs text-muted-foreground">
          {t('import.charCount', { count: rawText.length })}
        </p>
      </div>

      <Button onClick={onSubmit} disabled={!!validationError} className="w-full">
        {t('import.formatWithAI')}
      </Button>
    </div>
  )
}
