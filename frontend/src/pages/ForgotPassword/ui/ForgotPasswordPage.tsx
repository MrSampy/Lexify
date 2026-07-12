import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { ROUTES } from '@/shared/config'
import { ForgotPasswordForm } from '@/features/auth'

export function ForgotPasswordPage() {
  const { t } = useTranslation()
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
      <div style={{ width: '100%', maxWidth: 400 }}>
        <div className="lx-card" style={{ padding: 32, boxShadow: 'var(--shadow-2)' }}>
          <h1 className="ds-h3" style={{ margin: '0 0 6px', textAlign: 'center' }}>
            {t('auth.forgotTitle')}
          </h1>
          <p
            className="ds-sm"
            style={{ margin: '0 0 24px', color: 'var(--fg-3)', textAlign: 'center' }}
          >
            {t('auth.forgotDesc')}
          </p>

          <ForgotPasswordForm />

          <p className="ds-sm" style={{ margin: '20px 0 0', textAlign: 'center' }}>
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
