import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { ROUTES } from '@/shared/config'
import { Mascot } from '@/shared/ui'

export function NotFoundPage() {
  const { t } = useTranslation()
  return (
    <div
      style={{
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        minHeight: '60vh',
        gap: 14,
        textAlign: 'center',
        padding: 24,
      }}
    >
      <Mascot pose="lost" size={160} float />
      <div
        style={{
          fontFamily: 'var(--font-display)',
          fontSize: 'clamp(44px, 10vw, 72px)',
          fontWeight: 800,
          lineHeight: 1,
          color: 'var(--accent-color)',
        }}
      >
        404
      </div>
      <div className="ds-h3">{t('notFound.title')}</div>
      <p className="ds-body" style={{ color: 'var(--fg-3)', maxWidth: 380, margin: 0 }}>
        {t('notFound.description')}
      </p>
      <Link to={ROUTES.DASHBOARD} className="no-underline">
        <button className="lx-btn-primary" style={{ marginTop: 8 }}>
          {t('notFound.goHome')}
        </button>
      </Link>
    </div>
  )
}
