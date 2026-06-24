import { Spinner } from '@/shared/ui'
import { useImportWordsStore } from '../model/store'

export function FormatProgress() {
  const streamingText = useImportWordsStore((s) => s.streamingText)

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-3">
        <Spinner size="sm" />
        <p className="text-sm font-medium text-muted-foreground">AI is formatting your words…</p>
      </div>

      {streamingText && (
        <div className="max-h-72 overflow-y-auto rounded-lg border bg-muted/30 p-4">
          <pre className="whitespace-pre-wrap font-mono text-xs text-muted-foreground">
            {streamingText}
          </pre>
        </div>
      )}
    </div>
  )
}
