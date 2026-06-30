import { Link } from 'react-router-dom'
import { ROUTES } from '@/shared/config'
import { LoginForm } from '@/features/auth'

export function LoginPage() {
  return (
    <div
      style={{
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        padding: '24px 16px',
        background: 'var(--bg-1)',
        backgroundImage:
          'linear-gradient(var(--line-1) 1px, transparent 1px), linear-gradient(90deg, var(--line-1) 1px, transparent 1px)',
        backgroundSize: '64px 64px',
        position: 'relative',
        zIndex: 1,
      }}
    >
      <div style={{ width: '100%', maxWidth: 400 }}>
        {/* Logo */}
        <div style={{ display: 'flex', justifyContent: 'center', marginBottom: 34 }}>
          <span
            style={{
              fontFamily: 'var(--font-mono)',
              fontSize: 22,
              fontWeight: 700,
              letterSpacing: '-0.02em',
              color: 'var(--fg-1)',
            }}
          >
            <span style={{ color: 'var(--accent-color)' }}>&lt;</span>
            Lexify
            <span style={{ color: 'var(--accent-color)' }}>/&gt;</span>
          </span>
        </div>

        {/* Card */}
        <div
          style={{
            background: 'var(--bg-2)',
            border: '1px solid var(--line-2)',
            borderRadius: 'var(--r-lg)',
            padding: 30,
          }}
        >
          <div className="eyebrow" style={{ marginBottom: 14 }}>
            01 / AUTH
          </div>
          <h1 className="ds-h3" style={{ margin: '0 0 6px' }}>
            Sign in to Lexify
          </h1>
          <p className="ds-sm" style={{ margin: '0 0 24px', color: 'var(--fg-3)' }}>
            No account yet?{' '}
            <Link
              to={ROUTES.REGISTER}
              style={{ color: 'var(--accent-color)', textDecoration: 'none' }}
            >
              Create one →
            </Link>
          </p>

          <LoginForm />
        </div>

        <p className="ds-code" style={{ textAlign: 'center', color: 'var(--fg-4)', marginTop: 18 }}>
          // secured — JWT · global query filter
        </p>
      </div>
    </div>
  )
}
