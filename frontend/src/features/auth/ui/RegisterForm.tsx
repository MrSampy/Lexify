import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import { ROUTES } from '@/shared/config'
import { authApi } from '../api/authApi'

const schema = z.object({
  email: z.string().email('auth.emailInvalid'),
  password: z.string().min(8, 'auth.passwordMin'),
  displayName: z.string().min(2, 'auth.nameMin').max(50),
})

type FormValues = z.infer<typeof schema>

export function RegisterForm() {
  const { t } = useTranslation()
  const navigate = useNavigate()

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({ resolver: zodResolver(schema) })

  const onSubmit = async (values: FormValues) => {
    try {
      await authApi.register(values.email, values.password, values.displayName)
      navigate(ROUTES.LOGIN)
    } catch (err: unknown) {
      const message =
        (err as { response?: { data?: { message?: string } } })?.response?.data?.message ??
        'auth.registerFailed'
      setError('root', { message })
    }
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
