import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { Spinner } from '@/shared/ui'
import { useRequestEmailChangeMutation } from '@/entities/user'

interface ChangeEmailFormProps {
  currentEmail: string
  /** Address already awaiting confirmation, if a change is in flight. */
  pendingEmail: string | null
}

export function ChangeEmailForm({ currentEmail, pendingEmail }: ChangeEmailFormProps) {
  const { t } = useTranslation()
  const requestChange = useRequestEmailChangeMutation()

  const [newEmail, setNewEmail] = useState('')
  const [currentPassword, setCurrentPassword] = useState('')

  const submit = async () => {
    const email = newEmail.trim()
    if (!email || !currentPassword) {
      toast.error(t('profile.emailChangeIncomplete'))
      return
    }

    try {
      await requestChange.mutateAsync({ newEmail: email, currentPassword })
      toast.success(t('profile.emailChangeSent', { email }))
      setNewEmail('')
      setCurrentPassword('')
    } catch (err: unknown) {
      const message = (err as { response?: { data?: { message?: string } } })?.response?.data
        ?.message
      toast.error(message ?? t('profile.emailChangeFailed'))
    }
  }

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
      <p className="ds-sm" style={{ margin: 0, color: 'var(--fg-3)' }}>
        {t('profile.currentEmail')}: <strong>{currentEmail}</strong>
      </p>

      {pendingEmail && (
        <p
          className="ds-sm"
          style={{
            margin: 0,
            padding: '8px 12px',
            background: 'var(--bg-3)',
            border: '1px solid var(--line-2)',
            borderRadius: 'var(--r-sm)',
            color: 'var(--fg-2)',
            fontSize: 13,
          }}
        >
          {t('profile.emailChangePending', { email: pendingEmail })}
        </p>
      )}

      <input
        type="email"
        className="lx-input"
        autoComplete="email"
        placeholder={t('profile.newEmail')}
        aria-label={t('profile.newEmail')}
        value={newEmail}
        onChange={(e) => setNewEmail(e.target.value)}
      />

      <input
        type="password"
        className="lx-input"
        autoComplete="current-password"
        placeholder={t('profile.currentPassword')}
        aria-label={t('profile.currentPassword')}
        value={currentPassword}
        onChange={(e) => setCurrentPassword(e.target.value)}
      />

      <p style={{ margin: 0, color: 'var(--fg-4)', fontSize: 12 }}>
        {t('profile.emailChangeHint')}
      </p>

      <div>
        <button
          type="button"
          className="lx-btn-primary"
          style={{ padding: '9px 18px' }}
          disabled={requestChange.isPending}
          onClick={submit}
        >
          {requestChange.isPending ? <Spinner size="sm" /> : t('profile.emailChangeSubmit')}
        </button>
      </div>
    </div>
  )
}
