import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import { toast } from 'sonner'
import { ROUTES } from '@/shared/config'
import { authApi } from '../api/authApi'

const schema = z
  .object({
    newPassword: z.string().min(8, 'auth.passwordMin'),
    confirmPassword: z.string(),
  })
  .refine((v) => v.newPassword === v.confirmPassword, {
    message: 'auth.passwordsMismatch',
    path: ['confirmPassword'],
  })

type FormValues = z.infer<typeof schema>

interface ResetPasswordFormProps {
  token: string
}

export function ResetPasswordForm({ token }: ResetPasswordFormProps) {
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
      await authApi.resetPassword(token, values.newPassword)
      toast.success(t('auth.resetSuccess'))
      navigate(ROUTES.LOGIN)
    } catch {
      // The backend intentionally returns one generic message for any token failure
      setError('root', { message: 'auth.resetInvalid' })
    }
  }

  return (
    <form
      onSubmit={handleSubmit(onSubmit)}
      style={{ display: 'flex', flexDirection: 'column', gap: 16 }}
    >
      <div>
        <input
          id="newPassword"
          type="password"
          placeholder={t('auth.newPassword')}
          aria-label={t('auth.newPassword')}
          autoComplete="new-password"
          className="lx-input"
          {...register('newPassword')}
        />
        {errors.newPassword && (
          <p style={{ color: 'var(--danger)', fontSize: 13, marginTop: 4 }}>
            {t(errors.newPassword.message ?? '')}
          </p>
        )}
      </div>

      <div>
        <input
          id="confirmPassword"
          type="password"
          placeholder={t('auth.confirmPassword')}
          aria-label={t('auth.confirmPassword')}
          autoComplete="new-password"
          className="lx-input"
          {...register('confirmPassword')}
        />
        {errors.confirmPassword && (
          <p style={{ color: 'var(--danger)', fontSize: 13, marginTop: 4 }}>
            {t(errors.confirmPassword.message ?? '')}
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

      <button
        type="submit"
        className="lx-btn-primary"
        disabled={isSubmitting}
        style={{ width: '100%', justifyContent: 'center' }}
      >
        {isSubmitting ? t('auth.resetting') : t('auth.resetSubmit')}
      </button>
    </form>
  )
}
