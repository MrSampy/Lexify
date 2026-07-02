import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useNavigate } from 'react-router-dom'
import { ROUTES } from '@/shared/config'
import { authApi } from '../api/authApi'

const schema = z.object({
  email: z.string().email('Enter a valid email'),
  password: z.string().min(8, 'Password must be at least 8 characters'),
  displayName: z.string().min(2, 'At least 2 characters').max(50).optional(),
})

type FormValues = z.infer<typeof schema>

export function RegisterForm() {
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
        'Registration failed. Please try again.'
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
          placeholder="Your name (optional)"
          aria-label="Display name (optional)"
          {...register('displayName')}
        />
        {errors.displayName && (
          <p style={{ color: 'var(--danger)', fontSize: 13, marginTop: 4 }}>
            {errors.displayName.message}
          </p>
        )}
      </div>

      <div>
        <input
          id="email"
          type="email"
          placeholder="Email"
          aria-label="Email"
          autoComplete="email"
          className="lx-input"
          {...register('email')}
        />
        {errors.email && (
          <p style={{ color: 'var(--danger)', fontSize: 13, marginTop: 4 }}>
            {errors.email.message}
          </p>
        )}
      </div>

      <div>
        <input
          id="password"
          type="password"
          placeholder="Password"
          aria-label="Password"
          autoComplete="new-password"
          className="lx-input"
          {...register('password')}
        />
        {errors.password && (
          <p style={{ color: 'var(--danger)', fontSize: 13, marginTop: 4 }}>
            {errors.password.message}
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
          {errors.root.message}
        </div>
      )}

      <div style={{ height: 4 }} />

      <button
        type="submit"
        className="lx-btn-primary"
        disabled={isSubmitting}
        style={{ width: '100%', justifyContent: 'center' }}
      >
        {isSubmitting ? 'Creating account…' : 'Register'}
      </button>
    </form>
  )
}
