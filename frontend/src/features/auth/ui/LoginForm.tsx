import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useNavigate } from 'react-router-dom'
import { ROUTES } from '@/shared/config'
import { useAuthStore } from '@/entities/user'
import { authApi } from '../api/authApi'

const schema = z.object({
  email: z.string().email('Enter a valid email'),
  password: z.string().min(8, 'Password must be at least 8 characters'),
})

type FormValues = z.infer<typeof schema>

export function LoginForm() {
  const navigate = useNavigate()
  const setAuth = useAuthStore((s) => s.setAuth)

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({ resolver: zodResolver(schema) })

  const onSubmit = async (values: FormValues) => {
    try {
      const data = await authApi.login(values.email, values.password)
      setAuth(data)
      navigate(ROUTES.DASHBOARD)
    } catch (err: unknown) {
      const message =
        (err as { response?: { data?: { message?: string } } })?.response?.data?.message ??
        'Invalid email or password.'
      setError('root', { message })
    }
  }

  return (
    <form
      onSubmit={handleSubmit(onSubmit)}
      style={{ display: 'flex', flexDirection: 'column', gap: 16 }}
    >
      <div>
        <label className="lx-label" htmlFor="email">
          email
        </label>
        <input
          id="email"
          type="email"
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
        <label className="lx-label" htmlFor="password">
          password
        </label>
        <input
          id="password"
          type="password"
          autoComplete="current-password"
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
            fontFamily: 'var(--font-mono)',
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
        {isSubmitting ? 'Signing in…' : 'Sign in'}
      </button>
    </form>
  )
}
