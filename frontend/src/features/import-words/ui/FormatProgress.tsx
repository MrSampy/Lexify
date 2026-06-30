import { Spinner } from '@/shared/ui'
import { useImportWordsStore } from '../model/store'

export function FormatProgress() {
  const streamingText = useImportWordsStore((s) => s.streamingText)

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
        <Spinner size="sm" />
        <span style={{ color: 'var(--fg-3)', fontSize: 12, fontWeight: 600 }}>
          AI is formatting your words…
        </span>
      </div>

      {streamingText && (
        <div className="term" style={{ maxHeight: 280, overflowY: 'auto' }}>
          <pre
            style={{
              whiteSpace: 'pre-wrap',
              margin: 0,
              fontFamily: 'var(--font-body)',
              fontSize: 12,
              color: 'var(--fg-2)',
              lineHeight: 1.6,
            }}
          >
            {streamingText}
          </pre>
        </div>
      )}
    </div>
  )
}
