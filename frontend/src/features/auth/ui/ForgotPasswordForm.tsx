import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useTranslation } from 'react-i18next'
import { authApi } from '../api/authApi'

const schema = z.object({
  email: z.string().email('auth.emailInvalid'),
})

type FormValues = z.infer<typeof schema>

export function ForgotPasswordForm() {
  const { t } = useTranslation()
  const [sent, setSent] = useState(false)

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({ resolver: zodResolver(schema) })

  const onSubmit = async (values: FormValues) => {
    // The endpoint always returns 200, so any response means "if the account
    // exists, the email is on its way" — never reveal whether it does.
    await authApi.forgotPassword(values.email).catch(() => undefined)
    setSent(true)
  }

  if (sent) {
    return (
      <div
        style={{
          padding: '14px 16px',
          background: 'var(--accent-ghost)',
          border: '1px solid var(--accent-line)',
          borderRadius: 'var(--r-md)',
          color: 'var(--fg-2)',
          fontSize: 14,
          fontFamily: 'var(--font-body)',
          textAlign: 'center',
        }}
      >
        {t('auth.forgotSent')}
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

      <button
        type="submit"
        className="lx-btn-primary"
        disabled={isSubmitting}
        style={{ width: '100%', justifyContent: 'center' }}
      >
        {isSubmitting ? t('auth.sending') : t('auth.sendResetLink')}
      </button>
    </form>
  )
}
