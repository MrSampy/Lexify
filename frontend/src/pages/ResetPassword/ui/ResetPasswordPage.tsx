import { Link, useSearchParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { ROUTES } from '@/shared/config'
import { ResetPasswordForm } from '@/features/auth'

export function ResetPasswordPage() {
  const { t } = useTranslation()
  const [searchParams] = useSearchParams()
  const token = searchParams.get('token')

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
            {t('auth.resetTitle')}
          </h1>

          {token ? (
            <>
              <p
                className="ds-sm"
                style={{ margin: '0 0 24px', color: 'var(--fg-3)', textAlign: 'center' }}
              >
                {t('auth.resetDesc')}
              </p>
              <ResetPasswordForm token={token} />
            </>
          ) : (
            <p
              className="ds-sm"
              style={{ margin: '12px 0 0', color: 'var(--fg-3)', textAlign: 'center' }}
            >
              {t('auth.resetNoToken')}
            </p>
          )}

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
