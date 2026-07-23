import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { Spinner } from '@/shared/ui'
import { authApi } from '../api/authApi'

const COOLDOWN_SECONDS = 60

interface ResendTwoFactorButtonProps {
  challengeToken: string
}

/** Re-sends the sign-in code for an in-flight 2FA challenge. Cooldown mirrors ResendVerificationButton. */
export function ResendTwoFactorButton({ challengeToken }: ResendTwoFactorButtonProps) {
  const { t } = useTranslation()
  const [sending, setSending] = useState(false)
  const [cooldown, setCooldown] = useState(0)

  useEffect(() => {
    if (cooldown === 0) return
    const timer = setTimeout(() => setCooldown((s) => s - 1), 1000)
    return () => clearTimeout(timer)
  }, [cooldown])

  const resend = async () => {
    setSending(true)
    try {
      await authApi.resendTwoFactorCode(challengeToken)
      toast.success(t('auth.twoFactorResent'))
      setCooldown(COOLDOWN_SECONDS)
    } catch {
      toast.error(t('auth.twoFactorResendFailed'))
    } finally {
      setSending(false)
    }
  }

  const disabled = sending || cooldown > 0

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
        t('auth.twoFactorResendIn', { seconds: cooldown })
      ) : (
        t('auth.twoFactorResend')
      )}
    </button>
  )
}
