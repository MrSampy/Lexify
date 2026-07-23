import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import { ROUTES } from '@/shared/config'
import { authApi, type RegistrationStatus } from '../api/authApi'

const schema = z.object({
  email: z.string().email('auth.emailInvalid'),
  password: z.string().min(8, 'auth.passwordMin'),
  displayName: z.string().min(2, 'auth.nameMin').max(50),
  inviteCode: z.string().optional(),
})

type FormValues = z.infer<typeof schema>

export function RegisterForm() {
  const { t } = useTranslation()
  const navigate = useNavigate()

  // Until this resolves we don't know whether an invite code is needed, so the form renders as if
  // sign-up were open — the server is the real gate either way, this only shapes the UI.
  const [status, setStatus] = useState<RegistrationStatus | null>(null)

  useEffect(() => {
    authApi
      .registrationStatus()
      .then(setStatus)
      .catch(() => setStatus(null))
  }, [])

  const inviteRequired = status?.inviteRequired ?? false
  const signUpClosed = status !== null && !status.open && !status.inviteRequired

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({ resolver: zodResolver(schema) })

  const onSubmit = async (values: FormValues) => {
    try {
      await authApi.register(values.email, values.password, values.displayName, values.inviteCode)
      // When verification is off the account is already confirmed, so the check-email screen would be a
      // dead end (its resend button silently no-ops). Send them to login instead. When it's on (or the
      // status hasn't resolved), the check-email screen carries the address so it can offer a resend.
      if (status?.emailVerificationRequired === false) {
        navigate(ROUTES.LOGIN, { state: { registered: true } })
      } else {
        navigate(ROUTES.CHECK_EMAIL, { state: { email: values.email } })
      }
    } catch (err: unknown) {
      const message =
        (err as { response?: { data?: { message?: string } } })?.response?.data?.message ??
        'auth.registerFailed'
      setError('root', { message })
    }
  }

  if (signUpClosed) {
    return (
      <div
        style={{
          padding: '14px 16px',
          background: 'var(--bg-3)',
          border: '1px solid var(--line-2)',
          borderRadius: 'var(--r-md)',
          color: 'var(--fg-2)',
          fontSize: 14,
          fontFamily: 'var(--font-body)',
        }}
      >
        {t('auth.registrationClosed')}
      </div>
    )
  }

  return (
    <form
      onSubmit={handleSubmit(onSubmit)}
      style={{ display: 'flex', flexDirection: 'column', gap: 16 }}
    >
      <div>
        <input
          id="displayName"
          type="text"
          autoComplete="name"
          className="lx-input"
          placeholder={t('auth.yourName')}
          aria-label={t('auth.yourName')}
          {...register('displayName')}
        />
        {errors.displayName && (
          <p style={{ color: 'var(--danger)', fontSize: 13, marginTop: 4 }}>
            {t(errors.displayName.message ?? '')}
          </p>
        )}
      </div>

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
          autoComplete="new-password"
          className="lx-input"
          {...register('password')}
        />
        {errors.password && (
          <p style={{ color: 'var(--danger)', fontSize: 13, marginTop: 4 }}>
            {t(errors.password.message ?? '')}
          </p>
        )}
      </div>

      {inviteRequired && (
        <div>
          <input
            id="inviteCode"
            type="text"
            placeholder={t('auth.inviteCode')}
            aria-label={t('auth.inviteCode')}
            autoComplete="off"
            className="lx-input"
            {...register('inviteCode')}
          />
          <p style={{ color: 'var(--fg-4)', fontSize: 12, marginTop: 4 }}>
            {t('auth.inviteCodeHint')}
          </p>
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
        {isSubmitting ? t('auth.creatingAccount') : t('auth.register')}
      </button>
    </form>
  )
}
