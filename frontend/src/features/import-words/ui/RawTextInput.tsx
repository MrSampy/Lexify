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

interface Props {
  onSubmit: () => void
}

export function RawTextInput({ onSubmit }: Props) {
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
          <label className="mb-1 block text-sm font-medium">Target language (words to learn)</label>
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
          <label className="mb-1 block text-sm font-medium">Native language (translations)</label>
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
        <label className="mb-1 block text-sm font-medium">Your vocabulary text</label>
        <Textarea
          value={rawText}
          onChange={(e) => setRawText(e.target.value)}
          placeholder="Paste any text containing words you want to learn — a list, a paragraph, notes from a lesson..."
          className="min-h-[220px] resize-y font-mono text-sm"
          maxLength={10_000}
        />
        <p className="mt-1 text-xs text-muted-foreground">{rawText.length} / 10 000 characters</p>
      </div>

      <Button onClick={onSubmit} disabled={!!validationError} className="w-full">
        Format with AI
      </Button>
    </div>
  )
}
