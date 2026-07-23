import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { Spinner } from '@/shared/ui'
import { authApi } from '../api/authApi'

const COOLDOWN_SECONDS = 60

interface ResendVerificationButtonProps {
  email: string
}

/**
 * Requests a fresh confirmation link. The cooldown is a courtesy to stop impatient double-sends —
 * the server's auth rate limiter is the actual protection.
 */
export function ResendVerificationButton({ email }: ResendVerificationButtonProps) {
  const { t } = useTranslation()
  const [sending, setSending] = useState(false)
  const [cooldown, setCooldown] = useState(0)

  useEffect(() => {
    if (cooldown === 0) return
    const timer = setTimeout(() => setCooldown((s) => s - 1), 1000)
    return () => clearTimeout(timer)
  }, [cooldown])

  const resend = async () => {
    if (!email) return
    setSending(true)
    try {
      await authApi.resendVerification(email)
      // The endpoint is deliberately blind to whether the account exists, so the wording must be
      // too — promising "sent!" for an unknown address would leak exactly what it hides.
      toast.success(t('auth.verifyResent'))
      setCooldown(COOLDOWN_SECONDS)
    } catch {
      toast.error(t('auth.verifyResendFailed'))
    } finally {
      setSending(false)
    }
  }

  const disabled = sending || cooldown > 0 || !email

  return (
    <button
      type="button"
      onClick={resend}
      disabled={disabled}
      className="lx-btn-secondary"
      style={{ padding: '9px 16px', fontSize: 14, opacity: disabled ? 0.6 : 1 }}
    >
      {sending ? (
        <Spinner size="sm" />
      ) : cooldown > 0 ? (
        t('auth.verifyResendIn', { seconds: cooldown })
      ) : (
        t('auth.verifyResend')
      )}
    </button>
  )
}
