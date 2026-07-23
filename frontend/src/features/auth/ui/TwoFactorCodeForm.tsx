import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import type { AuthResponse } from '@/entities/user'
import { authApi, TWO_FACTOR_CHALLENGE_EXPIRED } from '../api/authApi'
import { ResendTwoFactorButton } from './ResendTwoFactorButton'

interface TwoFactorCodeFormProps {
  challengeToken: string
  onVerified: (auth: AuthResponse) => void
  /** The challenge expired (or is otherwise unusable) — the caller should return to the password step. */
  onExpired: () => void
}

/** Sign-in step 2: collect the emailed 6-digit code and exchange the challenge for a session. */
export function TwoFactorCodeForm({
  challengeToken,
  onVerified,
  onExpired,
}: TwoFactorCodeFormProps) {
  const { t } = useTranslation()
  const [code, setCode] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const submit = async (e: React.FormEvent) => {
    e.preventDefault()
    setSubmitting(true)
    setError(null)
    try {
      const auth = await authApi.verifyTwoFactor(challengeToken, code)
      onVerified(auth)
    } catch (err: unknown) {
      const response = (err as { response?: { status?: number; data?: { code?: string } } })
        ?.response
      // Only a dead challenge sends them back to sign in; a wrong/used code stays here (see below).
      if (response?.data?.code === TWO_FACTOR_CHALLENGE_EXPIRED) {
        onExpired()
        return
      }
      // The tight brute-force limiter answers 429 — say so rather than "wrong code".
      setError(response?.status === 429 ? 'auth.twoFactorRateLimited' : 'auth.twoFactorInvalid')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <form onSubmit={submit} style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <p className="ds-sm" style={{ margin: 0, color: 'var(--fg-3)' }}>
        {t('auth.twoFactorSent')}
      </p>

      <div>
        <input
          id="twoFactorCode"
          inputMode="numeric"
          autoComplete="one-time-code"
          maxLength={6}
          className="lx-input"
          placeholder={t('auth.twoFactorCodeLabel')}
          aria-label={t('auth.twoFactorCodeLabel')}
          value={code}
          onChange={(e) => setCode(e.target.value.replace(/\D/g, '').slice(0, 6))}
          autoFocus
          style={{ letterSpacing: '0.4em', fontFamily: 'var(--font-mono, monospace)' }}
        />
        {error && <p style={{ color: 'var(--danger)', fontSize: 13, marginTop: 4 }}>{t(error)}</p>}
      </div>

      <button
        type="submit"
        className="lx-btn-primary"
        disabled={submitting || code.length !== 6}
        style={{ width: '100%', justifyContent: 'center' }}
      >
        {submitting ? t('auth.twoFactorVerifying') : t('auth.twoFactorVerify')}
      </button>

      <div style={{ display: 'flex', justifyContent: 'center' }}>
        <ResendTwoFactorButton challengeToken={challengeToken} />
      </div>
    </form>
  )
}
