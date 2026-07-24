import { useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { ROUTES } from '@/shared/config'
import { Mascot, Spinner } from '@/shared/ui'
import { unsubscribeApi } from '@/features/unsubscribe'

type State = 'idle' | 'submitting' | 'done' | 'error'

export function UnsubscribePage() {
  const { t } = useTranslation()
  const [searchParams] = useSearchParams()
  const token = searchParams.get('token')

  const [state, setState] = useState<State>(() => (token ? 'idle' : 'error'))

  // Deliberately behind a button rather than an effect: mail providers and corporate scanners
  // pre-fetch every link in a message, which would silently unsubscribe people who never clicked.
  const handleConfirm = async () => {
    if (!token) return
    setState('submitting')
    try {
      await unsubscribeApi.unsubscribe(token)
      setState('done')
    } catch {
      setState('error')
    }
  }

  return (
    <div
      style={{
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        padding: '24px 16px',
        background: 'var(--bg-0)',
      }}
    >
      <div style={{ width: '100%', maxWidth: 420 }}>
        <div
          className="lx-card"
          style={{ padding: 32, boxShadow: 'var(--shadow-2)', textAlign: 'center' }}
        >
          {(state === 'idle' || state === 'submitting') && (
            <>
              <Mascot pose="sleep" size={112} />
              <h1 className="ds-h3" style={{ margin: '12px 0 6px' }}>
                {t('unsubscribe.title')}
              </h1>
              <p className="ds-sm" style={{ margin: '0 0 20px', color: 'var(--fg-3)' }}>
                {t('unsubscribe.body')}
              </p>
              <button
                className="lx-btn-primary"
                onClick={() => void handleConfirm()}
                disabled={state === 'submitting'}
              >
                {state === 'submitting' ? <Spinner size="sm" /> : t('unsubscribe.confirm')}
              </button>
            </>
          )}

          {state === 'done' && (
            <>
              <Mascot pose="farewell" size={112} />
              <h1 className="ds-h3" style={{ margin: '12px 0 6px' }}>
                {t('unsubscribe.doneTitle')}
              </h1>
              <p className="ds-sm" style={{ margin: '0 0 20px', color: 'var(--fg-3)' }}>
                {t('unsubscribe.doneBody')}
              </p>
              <Link
                to={ROUTES.PROFILE}
                className="lx-btn-secondary"
                style={{ display: 'inline-block', padding: '10px 22px', textDecoration: 'none' }}
              >
                {t('unsubscribe.openProfile')}
              </Link>
            </>
          )}

          {state === 'error' && (
            <>
              <Mascot pose="confused" size={104} />
              <h1 className="ds-h3" style={{ margin: '12px 0 6px' }}>
                {t('unsubscribe.failedTitle')}
              </h1>
              <p className="ds-sm" style={{ margin: '0 0 16px', color: 'var(--fg-3)' }}>
                {t('unsubscribe.failedBody')}
              </p>
              <p className="ds-sm" style={{ margin: '20px 0 0' }}>
                <Link
                  to={ROUTES.LOGIN}
                  style={{ color: 'var(--accent-color)', textDecoration: 'none', fontWeight: 700 }}
                >
                  {t('auth.backToLogin')}
                </Link>
              </p>
            </>
          )}
        </div>
      </div>
    </div>
  )
}
