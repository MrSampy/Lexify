import { Link } from 'react-router-dom'
import { ROUTES } from '@/shared/config'
import { RegisterForm } from '@/features/auth'

export function RegisterPage() {
  return (
    <div className="flex min-h-screen items-center justify-center bg-background px-4">
      <div className="w-full max-w-sm space-y-6">
        <div className="space-y-1 text-center">
          <h1 className="text-2xl font-bold tracking-tight">Create your account</h1>
          <p className="text-sm text-muted-foreground">
            Already have an account?{' '}
            <Link to={ROUTES.LOGIN} className="text-primary underline-offset-4 hover:underline">
              Sign in
            </Link>
          </p>
        </div>

        <div className="rounded-xl border bg-card p-6 shadow-sm">
          <RegisterForm />
        </div>
      </div>
    </div>
  )
}
