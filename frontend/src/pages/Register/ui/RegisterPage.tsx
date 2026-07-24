import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { ROUTES } from '@/shared/config'
import { Mascot } from '@/shared/ui'
import { RegisterForm } from '@/features/auth'

export function RegisterPage() {
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
        <div style={{ display: 'flex', justifyContent: 'center', marginBottom: 4 }}>
          <Mascot pose="neutral" size={110} float />
        </div>
        {/* Logo */}
        <div
          style={{
            display: 'flex',
            justifyContent: 'center',
            marginBottom: 32,
            gap: 12,
            alignItems: 'center',
          }}
        >
          <div
            style={{
              width: 40,
              height: 40,
              background: 'var(--accent-color)',
              borderRadius: 'var(--r-sm)',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              boxShadow: '0 6px 16px rgba(22,185,129,0.30)',
            }}
          >
            <span
              style={{
                color: '#fff',
                fontSize: 20,
                fontWeight: 800,
                fontFamily: 'var(--font-display)',
              }}
            >
              L
            </span>
          </div>
          <span
            style={{
              fontFamily: 'var(--font-display)',
              fontSize: 26,
              fontWeight: 700,
              color: 'var(--fg-1)',
            }}
          >
            Lexify
          </span>
        </div>

        {/* Card */}
        <div className="lx-card" style={{ padding: 32, boxShadow: 'var(--shadow-2)' }}>
          <h1 className="ds-h3" style={{ margin: '0 0 6px', textAlign: 'center' }}>
            {t('auth.createTitle')}
          </h1>
          <p
            className="ds-sm"
            style={{ margin: '0 0 24px', color: 'var(--fg-3)', textAlign: 'center' }}
          >
            {t('auth.alreadyRegistered')}{' '}
            <Link
              to={ROUTES.LOGIN}
              style={{ color: 'var(--accent-color)', textDecoration: 'none', fontWeight: 700 }}
            >
              {t('auth.signIn')}
            </Link>
          </p>

          <RegisterForm />
        </div>
      </div>
    </div>
  )
}
