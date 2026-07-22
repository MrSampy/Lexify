import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import { ROUTES } from '@/shared/config'
import { Mascot, Spinner } from '@/shared/ui'
import { useBlocks } from '@/entities/block'
import { useStartConversationMutation } from '@/entities/conversation'

const ALL_BLOCKS = '__all__'

export function ConversationSetupPage() {
  const { t, i18n } = useTranslation()
  const navigate = useNavigate()
  const nativeLanguage = i18n.resolvedLanguage === 'uk' ? 'Ukrainian' : 'English'

  const [blockId, setBlockId] = useState<string>(ALL_BLOCKS)
  const [scenario, setScenario] = useState('')
  const [error, setError] = useState<string | null>(null)

  const { data: blocks, isLoading } = useBlocks({ page: 1, pageSize: 100 })
  const start = useStartConversationMutation()

  const handleStart = async () => {
    setError(null)
    try {
      const result = await start.mutateAsync({
        blockId: blockId === ALL_BLOCKS ? undefined : blockId,
        scenario: scenario.trim() || undefined,
        nativeLanguage,
      })
      navigate(ROUTES.PRACTICE_CHAT_SESSION(result.conversationId), { state: { start: result } })
    } catch (err) {
      const message =
        (err as { response?: { data?: { message?: string } } })?.response?.data?.message ??
        t('chat.errStart')
      setError(message)
    }
  }

  return (
    <div style={{ maxWidth: 640, margin: '0 auto' }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 14, marginBottom: 8 }}>
        <Mascot pose="greeting" size={64} float />
        <div>
          <h1 className="ds-h2" style={{ margin: 0 }}>
            {t('chat.title')}
          </h1>
          <p className="ds-sm" style={{ color: 'var(--fg-3)', margin: '4px 0 0' }}>
            {t('chat.subtitle')}
          </p>
        </div>
      </div>

      <div
        style={{
          background: 'var(--bg-2)',
          border: '1px solid var(--line-2)',
          borderRadius: 'var(--r-lg)',
          padding: 'clamp(18px, 4vw, 28px)',
          marginTop: 20,
          display: 'flex',
          flexDirection: 'column',
          gap: 18,
        }}
      >
        {/* Scope */}
        <div>
          <label
            style={{
              display: 'block',
              fontSize: 13,
              fontWeight: 700,
              color: 'var(--fg-2)',
              marginBottom: 6,
            }}
          >
            {t('chat.scopeLabel')}
          </label>
          {isLoading ? (
            <Spinner size="sm" />
          ) : (
            <select
              value={blockId}
              onChange={(e) => setBlockId(e.target.value)}
              style={{
                width: '100%',
                padding: '10px 12px',
                borderRadius: 'var(--r-md)',
                border: '1.5px solid var(--line-2)',
                background: 'var(--bg-1)',
                color: 'var(--fg-1)',
                fontSize: 14,
              }}
            >
              <option value={ALL_BLOCKS}>{t('chat.scopeAllDue')}</option>
              {blocks?.items.map((b) => (
                <option key={b.id} value={b.id}>
                  {b.title} ({b.wordCount})
                </option>
              ))}
            </select>
          )}
        </div>

        {/* Scenario */}
        <div>
          <label
            style={{
              display: 'block',
              fontSize: 13,
              fontWeight: 700,
              color: 'var(--fg-2)',
              marginBottom: 6,
            }}
          >
            {t('chat.scenarioLabel')}
          </label>
          <input
            value={scenario}
            onChange={(e) => setScenario(e.target.value)}
            maxLength={200}
            placeholder={t('chat.scenarioPlaceholder')}
            style={{
              width: '100%',
              padding: '10px 12px',
              borderRadius: 'var(--r-md)',
              border: '1.5px solid var(--line-2)',
              background: 'var(--bg-1)',
              color: 'var(--fg-1)',
              fontSize: 14,
            }}
          />
        </div>

        {error && (
          <div style={{ color: 'var(--danger)', fontSize: 13, fontWeight: 600 }}>{error}</div>
        )}

        <button
          className="lx-btn-primary"
          onClick={() => void handleStart()}
          disabled={start.isPending}
          style={{ alignSelf: 'flex-start' }}
        >
          {start.isPending ? t('chat.starting') : t('chat.start')}
        </button>
      </div>
    </div>
  )
}
