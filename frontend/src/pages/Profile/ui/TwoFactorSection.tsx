import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import {
  useEnableTwoFactorMutation,
  useResendEnableTwoFactorMutation,
  useConfirmTwoFactorMutation,
  useDisableTwoFactorMutation,
} from '@/entities/user'

interface TwoFactorSectionProps {
  enabled: boolean
  mandatory: boolean
}

/** Profile control for the email-code second factor: enable (with code confirm) / disable / locked-on. */
export function TwoFactorSection({ enabled, mandatory }: TwoFactorSectionProps) {
  const { t } = useTranslation()
  const [mode, setMode] = useState<'idle' | 'enrolling' | 'disabling'>('idle')
  const [code, setCode] = useState('')
  const [password, setPassword] = useState('')

  const enable = useEnableTwoFactorMutation()
  const resend = useResendEnableTwoFactorMutation()
  const confirm = useConfirmTwoFactorMutation()
  const disable = useDisableTwoFactorMutation()

  const reset = () => {
    setMode('idle')
    setCode('')
    setPassword('')
  }

  const startEnroll = async () => {
    try {
      await enable.mutateAsync()
      setMode('enrolling')
    } catch {
      toast.error(t('profile.twoFactorEnableFailed'))
    }
  }

  const confirmEnroll = async () => {
    try {
      await confirm.mutateAsync(code)
      toast.success(t('profile.twoFactorEnabledToast'))
      reset()
    } catch {
      toast.error(t('auth.twoFactorInvalid'))
    }
  }

  const confirmDisable = async () => {
    try {
      await disable.mutateAsync(password)
      toast.success(t('profile.twoFactorDisabledToast'))
      reset()
    } catch {
      toast.error(t('profile.twoFactorDisableFailed'))
    }
  }

  if (mandatory) {
    return (
      <p className="ds-sm" style={{ margin: 0, color: 'var(--fg-3)' }}>
        {t('profile.twoFactorMandatory')}
      </p>
    )
  }

  // Enrolling: the code was emailed; collect and confirm it.
  if (mode === 'enrolling') {
    return (
      <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
        <p className="ds-sm" style={{ margin: 0, color: 'var(--fg-3)' }}>
          {t('auth.twoFactorSent')}
        </p>
        <input
          className="lx-input"
          inputMode="numeric"
          autoComplete="one-time-code"
          maxLength={6}
          placeholder={t('auth.twoFactorCodeLabel')}
          value={code}
          onChange={(e) => setCode(e.target.value.replace(/\D/g, '').slice(0, 6))}
          style={{ letterSpacing: '0.4em', fontFamily: 'var(--font-mono, monospace)' }}
        />
        <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
          <button
            className="lx-btn-primary"
            onClick={() => void confirmEnroll()}
            disabled={confirm.isPending || code.length !== 6}
          >
            {t('profile.twoFactorConfirmCode')}
          </button>
          <button
            className="lx-btn-secondary"
            onClick={() =>
              void resend.mutateAsync().then(() => toast.success(t('auth.twoFactorResent')))
            }
            disabled={resend.isPending}
          >
            {t('auth.twoFactorResend')}
          </button>
          <button className="lx-btn-secondary" onClick={reset}>
            {t('common.cancel')}
          </button>
        </div>
      </div>
    )
  }

  // Disabling: re-authenticate with the current password.
  if (mode === 'disabling') {
    return (
      <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
        <input
          className="lx-input"
          type="password"
          autoComplete="current-password"
          placeholder={t('profile.twoFactorCurrentPassword')}
          value={password}
          onChange={(e) => setPassword(e.target.value)}
        />
        <div style={{ display: 'flex', gap: 8 }}>
          <button
            className="lx-btn-primary"
            onClick={() => void confirmDisable()}
            disabled={disable.isPending || !password}
          >
            {t('profile.twoFactorDisable')}
          </button>
          <button className="lx-btn-secondary" onClick={reset}>
            {t('common.cancel')}
          </button>
        </div>
      </div>
    )
  }

  // Idle: show status + the primary action.
  return (
    <div
      style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 12 }}
    >
      <span className="ds-sm" style={{ color: 'var(--fg-2)' }}>
        {enabled ? t('profile.twoFactorOn') : t('profile.twoFactorOff')}
      </span>
      {enabled ? (
        <button className="lx-btn-secondary" onClick={() => setMode('disabling')}>
          {t('profile.twoFactorDisable')}
        </button>
      ) : (
        <button
          className="lx-btn-primary"
          onClick={() => void startEnroll()}
          disabled={enable.isPending}
        >
          {t('profile.twoFactorEnable')}
        </button>
      )}
    </div>
  )
}
