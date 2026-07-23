import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Copy, Check } from 'lucide-react'
import { Button, Mascot } from '@/shared/ui'
import { FeedbackForm } from '@/features/submit-feedback'
import type { SubmitFeedbackResult } from '@/entities/feedback'

function SuccessPanel({ result, onAgain }: { result: SubmitFeedbackResult; onAgain: () => void }) {
  const { t } = useTranslation()
  const [copied, setCopied] = useState(false)

  const copy = async () => {
    await navigator.clipboard.writeText(result.ticketCode)
    setCopied(true)
    setTimeout(() => setCopied(false), 2000)
  }

  return (
    <div style={{ display: 'grid', gap: 16, justifyItems: 'center', textAlign: 'center' }}>
      <Mascot pose="celebrate" size={120} />
      <h2 className="ds-h2" style={{ margin: 0 }}>
        {t('feedback.successTitle')}
      </h2>
      <p className="ds-sm" style={{ color: 'var(--fg-3)', margin: 0, maxWidth: 420 }}>
        {t('feedback.successBody')}
      </p>

      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          gap: 10,
          padding: '10px 16px',
          background: 'var(--bg-3)',
          border: '1px solid var(--line-2)',
          borderRadius: 'var(--r-md)',
        }}
      >
        <span className="ds-code" style={{ fontSize: 18, fontWeight: 800 }}>
          {result.ticketCode}
        </span>
        <button
          type="button"
          onClick={copy}
          aria-label={t('feedback.copyTicket')}
          style={{
            display: 'flex',
            border: 'none',
            background: 'transparent',
            color: copied ? 'var(--success)' : 'var(--fg-3)',
            cursor: 'pointer',
          }}
        >
          {copied ? (
            <Check style={{ width: 18, height: 18 }} />
          ) : (
            <Copy style={{ width: 18, height: 18 }} />
          )}
        </button>
      </div>

      <Button variant="outline" onClick={onAgain}>
        {t('feedback.sendAnother')}
      </Button>
    </div>
  )
}

export function FeedbackPage() {
  const { t } = useTranslation()
  const [result, setResult] = useState<SubmitFeedbackResult | null>(null)

  return (
    <div style={{ maxWidth: 640, margin: '0 auto' }}>
      {result ? (
        <SuccessPanel result={result} onAgain={() => setResult(null)} />
      ) : (
        <>
          <div style={{ display: 'flex', alignItems: 'center', gap: 14, marginBottom: 24 }}>
            <Mascot pose="greeting" size={64} float />
            <div style={{ flex: 1 }}>
              <h1 className="ds-h2" style={{ margin: 0 }}>
                {t('feedback.title')}
              </h1>
              <p className="ds-sm" style={{ color: 'var(--fg-3)', margin: '4px 0 0' }}>
                {t('feedback.subtitle')}
              </p>
            </div>
          </div>

          <FeedbackForm onSubmitted={setResult} />
        </>
      )}
    </div>
  )
}
