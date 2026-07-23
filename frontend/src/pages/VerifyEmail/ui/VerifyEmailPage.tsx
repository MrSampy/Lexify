import { useEffect, useRef, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { ROUTES } from '@/shared/config'
import { Mascot, Spinner } from '@/shared/ui'
import { authApi, ResendVerificationButton } from '@/features/auth'

type State =
  | { status: 'verifying' }
  | { status: 'success'; email: string; emailChanged: boolean }
  | { status: 'error' }

export function VerifyEmailPage() {
  const { t } = useTranslation()
  const [searchParams] = useSearchParams()
  const token = searchParams.get('token')

  const [state, setState] = useState<State>(() =>
    token ? { status: 'verifying' } : { status: 'error' },
  )
  const [email, setEmail] = useState('')

  // The token is single-use, and StrictMode runs effects twice in dev — without this guard the
  // second call consumes nothing and paints "invalid link" over a successful confirmation.
  const attempted = useRef(false)

  useEffect(() => {
    if (!token || attempted.current) return
    attempted.current = true

    authApi
      .verifyEmail(token)
      .then((result) =>
        setState({ status: 'success', email: result.email, emailChanged: result.emailChanged }),
      )
      .catch(() => setState({ status: 'error' }))
  }, [token])

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
          {state.status === 'verifying' && (
            <>
              <Spinner size="lg" />
              <p className="ds-sm" style={{ margin: '16px 0 0', color: 'var(--fg-3)' }}>
                {t('auth.verifyChecking')}
              </p>
            </>
          )}

          {state.status === 'success' && (
            <>
              <Mascot pose="celebrate" size={112} />
              <h1 className="ds-h3" style={{ margin: '12px 0 6px' }}>
                {t('auth.verifySuccessTitle')}
              </h1>
              <p className="ds-sm" style={{ margin: '0 0 20px', color: 'var(--fg-3)' }}>
                {state.emailChanged
                  ? t('auth.verifyEmailChanged', { email: state.email })
                  : t('auth.verifySuccessBody')}
              </p>
              <Link
                to={ROUTES.LOGIN}
                className="lx-btn-primary"
                style={{ display: 'inline-block', padding: '10px 22px', textDecoration: 'none' }}
              >
                {t('auth.signIn')}
              </Link>
            </>
          )}

          {state.status === 'error' && (
            <>
              <Mascot pose="confused" size={104} />
              <h1 className="ds-h3" style={{ margin: '12px 0 6px' }}>
                {t('auth.verifyFailedTitle')}
              </h1>
              <p className="ds-sm" style={{ margin: '0 0 16px', color: 'var(--fg-3)' }}>
                {t('auth.verifyFailedBody')}
              </p>

              <div
                style={{ display: 'flex', flexDirection: 'column', gap: 10, alignItems: 'center' }}
              >
                <input
                  type="email"
                  className="lx-input"
                  placeholder={t('auth.email')}
                  aria-label={t('auth.email')}
                  autoComplete="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                />
                <ResendVerificationButton email={email} />
              </div>

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
