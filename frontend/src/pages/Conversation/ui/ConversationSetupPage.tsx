import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate } from 'react-router-dom'
import { ROUTES } from '@/shared/config'
import { Mascot, Spinner } from '@/shared/ui'
import { useBlocks } from '@/entities/block'
import { useStartConversationMutation } from '@/entities/conversation'
import { SCENARIO_PRESETS } from '../model/scenarioPresets'

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
        <div style={{ flex: 1 }}>
          <h1 className="ds-h2" style={{ margin: 0 }}>
            {t('chat.title')}
          </h1>
          <p className="ds-sm" style={{ color: 'var(--fg-3)', margin: '4px 0 0' }}>
            {t('chat.subtitle')}
          </p>
        </div>
        <Link
          to={ROUTES.PRACTICE_CHAT_HISTORY}
          style={{
            color: 'var(--accent-color)',
            textDecoration: 'none',
            fontSize: 14,
            fontWeight: 700,
            flexShrink: 0,
          }}
        >
          {t('chat.history')}
        </Link>
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
          {/* Preset chips fill the input; editing the text simply diverges from the preset. */}
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: 6, marginBottom: 8 }}>
            {SCENARIO_PRESETS.map((p) => {
              const selected = scenario === p.text
              return (
                <button
                  key={p.id}
                  type="button"
                  aria-pressed={selected}
                  onClick={() => setScenario(selected ? '' : p.text)}
                  style={{
                    fontSize: 12,
                    fontWeight: 700,
                    padding: '4px 10px',
                    borderRadius: 'var(--r-pill)',
                    border: `1.5px solid ${selected ? 'var(--accent-color)' : 'var(--line-2)'}`,
                    background: selected ? 'var(--accent-ghost)' : 'var(--bg-1)',
                    color: selected ? 'var(--accent-dim)' : 'var(--fg-3)',
                    cursor: 'pointer',
                  }}
                >
                  {p.emoji} {t(p.labelKey)}
                </button>
              )
            })}
          </div>
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
