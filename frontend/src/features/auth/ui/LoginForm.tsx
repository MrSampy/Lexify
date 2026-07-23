import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useTranslation } from 'react-i18next'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { useQueryClient } from '@tanstack/react-query'
import { ROUTES } from '@/shared/config'
import { useAuthStore, type AuthResponse } from '@/entities/user'
import { authApi, EMAIL_NOT_VERIFIED, isTwoFactorChallenge } from '../api/authApi'
import { ResendVerificationButton } from './ResendVerificationButton'
import { TwoFactorCodeForm } from './TwoFactorCodeForm'

const schema = z.object({
  email: z.string().email('auth.emailInvalid'),
  password: z.string().min(8, 'auth.passwordMin'),
})

type FormValues = z.infer<typeof schema>

export function LoginForm() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const location = useLocation()
  const queryClient = useQueryClient()
  const setAuth = useAuthStore((s) => s.setAuth)

  // Set by RegisterForm when the account was auto-confirmed (verification off) and the user was routed
  // here instead of the check-email screen — show a one-line success notice so the bare form isn't jarring.
  const justRegistered = (location.state as { registered?: boolean } | null)?.registered === true

  // Set only when the server refused the sign-in for lack of confirmation, so the notice below
  // appears exactly in that case.
  const [unverifiedEmail, setUnverifiedEmail] = useState<string | null>(null)

  // Set when sign-in step 1 returned a 2FA challenge; the code-entry form replaces the credentials form.
  const [challengeToken, setChallengeToken] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({ resolver: zodResolver(schema) })

  const completeSignIn = (auth: AuthResponse) => {
    // Drop any queries cached for a previously logged-in user before this
    // identity's screens mount, so no stale data flashes or leaks through.
    queryClient.clear()
    setAuth(auth)
    navigate(ROUTES.DASHBOARD)
  }

  const onSubmit = async (values: FormValues) => {
    try {
      const data = await authApi.login(values.email, values.password)
      if (isTwoFactorChallenge(data)) {
        setUnverifiedEmail(null)
        setChallengeToken(data.challengeToken)
        return
      }
      completeSignIn(data)
    } catch (err: unknown) {
      const data = (err as { response?: { data?: { message?: string; code?: string } } })?.response
        ?.data

      // Unconfirmed address: show the resend affordance instead of a dead-end credentials error.
      if (data?.code === EMAIL_NOT_VERIFIED) {
        setUnverifiedEmail(values.email)
        return
      }

      setUnverifiedEmail(null)
      setError('root', { message: data?.message ?? 'auth.invalidCredentials' })
    }
  }

  if (challengeToken) {
    return (
      <TwoFactorCodeForm
        challengeToken={challengeToken}
        onVerified={completeSignIn}
        onExpired={() => setChallengeToken(null)}
      />
    )
  }

  return (
    <form
      onSubmit={handleSubmit(onSubmit)}
      style={{ display: 'flex', flexDirection: 'column', gap: 16 }}
    >
      {justRegistered && !unverifiedEmail && (
        <div
          role="status"
          style={{
            padding: '10px 14px',
            background: 'var(--bg-3)',
            border: '1px solid var(--line-2)',
            borderRadius: 'var(--r-md)',
            color: 'var(--fg-2)',
            fontSize: 13,
          }}
        >
          {t('auth.registerSuccess')}
        </div>
      )}

      <div>
        <input
          id="email"
          type="email"
          placeholder={t('auth.email')}
          aria-label={t('auth.email')}
          autoComplete="email"
          className="lx-input"
          {...register('email')}
        />
        {errors.email && (
          <p style={{ color: 'var(--danger)', fontSize: 13, marginTop: 4 }}>
            {t(errors.email.message ?? '')}
          </p>
        )}
      </div>

      <div>
        <input
          id="password"
          type="password"
          placeholder={t('auth.password')}
          aria-label={t('auth.password')}
          autoComplete="current-password"
          className="lx-input"
          {...register('password')}
        />
        {errors.password && (
          <p style={{ color: 'var(--danger)', fontSize: 13, marginTop: 4 }}>
            {t(errors.password.message ?? '')}
          </p>
        )}
        <p style={{ margin: '6px 0 0', textAlign: 'right' }}>
          <Link
            to={ROUTES.FORGOT_PASSWORD}
            style={{
              color: 'var(--accent-color)',
              textDecoration: 'none',
              fontSize: 13,
              fontWeight: 600,
            }}
          >
            {t('auth.forgotPassword')}
          </Link>
        </p>
      </div>

      {unverifiedEmail && (
        <div
          role="alert"
          style={{
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'flex-start',
            gap: 10,
            padding: '12px 14px',
            background: 'var(--bg-3)',
            border: '1px solid var(--line-2)',
            borderRadius: 'var(--r-md)',
            color: 'var(--fg-2)',
            fontSize: 13,
          }}
        >
          <span>{t('auth.verifyRequired')}</span>
          <ResendVerificationButton email={unverifiedEmail} />
        </div>
      )}

      {errors.root && (
        <div
          style={{
            padding: '10px 14px',
            background: 'var(--danger-ghost)',
            border: '1px solid rgba(255,92,108,0.3)',
            borderRadius: 'var(--r-md)',
            color: 'var(--danger)',
            fontSize: 13,
            fontFamily: 'var(--font-body)',
          }}
        >
          {t(errors.root.message ?? '')}
        </div>
      )}

      <div style={{ height: 4 }} />

      <button
        type="submit"
        className="lx-btn-primary"
        disabled={isSubmitting}
        style={{ width: '100%', justifyContent: 'center' }}
      >
        {isSubmitting ? t('auth.signingIn') : t('auth.signIn')}
      </button>
    </form>
  )
}
