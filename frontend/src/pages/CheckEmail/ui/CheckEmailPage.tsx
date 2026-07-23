import { Link, useLocation } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { ROUTES } from '@/shared/config'
import { Mascot } from '@/shared/ui'
import { ResendVerificationButton } from '@/features/auth'

/** Landing spot right after sign-up: the account exists but is waiting on the confirmation link. */
export function CheckEmailPage() {
  const { t } = useTranslation()
  const location = useLocation()

  // Passed by RegisterForm. Absent on a direct visit — then there is nothing to resend to, and the
  // button stays disabled rather than guessing an address.
  const email = (location.state as { email?: string } | null)?.email ?? ''

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
          <Mascot pose="pointing" size={104} />

          <h1 className="ds-h3" style={{ margin: '12px 0 6px' }}>
            {t('auth.checkEmailTitle')}
          </h1>

          <p className="ds-sm" style={{ margin: '0 0 8px', color: 'var(--fg-3)' }}>
            {email ? t('auth.checkEmailSentTo', { email }) : t('auth.checkEmailGeneric')}
          </p>

          <p className="ds-sm" style={{ margin: '0 0 20px', color: 'var(--fg-4)', fontSize: 13 }}>
            {t('auth.checkEmailSpamHint')}
          </p>

          {email && (
            <div style={{ display: 'flex', justifyContent: 'center' }}>
              <ResendVerificationButton email={email} />
            </div>
          )}

          <p className="ds-sm" style={{ margin: '20px 0 0' }}>
            <Link
              to={ROUTES.LOGIN}
              style={{ color: 'var(--accent-color)', textDecoration: 'none', fontWeight: 700 }}
            >
              {t('auth.backToLogin')}
            </Link>
          </p>
        </div>
      </div>
    </div>
  )
}
