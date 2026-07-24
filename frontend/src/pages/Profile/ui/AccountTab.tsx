import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import {
  useUpdateDisplayNameMutation,
  useChangePasswordMutation,
  type Profile,
} from '@/entities/user'
import { ChangeEmailForm } from '@/features/change-email'
import { SectionCard } from './SectionCard'
import { TwoFactorSection } from './TwoFactorSection'

/** Who the account belongs to and how it is secured: name, address, second factor, password. */
export function AccountTab({ profile }: { profile: Profile }) {
  const { t } = useTranslation()

  const updateDisplayName = useUpdateDisplayNameMutation()
  const changePassword = useChangePasswordMutation()

  // null = untouched by the user → show the profile value; avoids setState-in-effect
  const [displayNameDraft, setDisplayNameDraft] = useState<string | null>(null)
  const displayName = displayNameDraft ?? profile.displayName ?? ''

  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')

  const handleSaveDisplayName = async () => {
    try {
      await updateDisplayName.mutateAsync(displayName.trim() === '' ? null : displayName.trim())
      setDisplayNameDraft(null)
      toast.success(t('profile.nameUpdated'))
    } catch {
      toast.error(t('profile.nameUpdateFailed'))
    }
  }

  const handleChangePassword = async () => {
    if (newPassword !== confirmPassword) {
      toast.error(t('profile.mismatch'))
      return
    }
    if (newPassword.length < 8) {
      toast.error(t('profile.tooShort'))
      return
    }
    try {
      await changePassword.mutateAsync({ currentPassword, newPassword })
      setCurrentPassword('')
      setNewPassword('')
      setConfirmPassword('')
      toast.success(t('profile.changed'))
    } catch {
      toast.error(t('profile.changeFailed'))
    }
  }

  return (
    <>
      <SectionCard title={t('profile.displayName')}>
        <div style={{ display: 'flex', gap: 10, flexWrap: 'wrap' }}>
          <input
            className="lx-input"
            style={{ flex: '1 1 200px', minWidth: 0 }}
            value={displayName}
            placeholder={t('profile.namePlaceholder')}
            maxLength={64}
            onChange={(e) => setDisplayNameDraft(e.target.value)}
          />
          <button
            className="lx-btn-primary"
            onClick={() => void handleSaveDisplayName()}
            disabled={
              updateDisplayName.isPending || (profile.displayName ?? '') === displayName.trim()
            }
          >
            {t('common.save')}
          </button>
        </div>
      </SectionCard>

      <SectionCard title={t('profile.changeEmail')}>
        <ChangeEmailForm currentEmail={profile.email} pendingEmail={profile.pendingEmail} />
      </SectionCard>

      <SectionCard title={t('profile.twoFactor')}>
        <TwoFactorSection
          enabled={profile.twoFactorEnabled}
          mandatory={profile.twoFactorMandatory}
        />
      </SectionCard>

      <SectionCard title={t('profile.changePassword')}>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
          <input
            className="lx-input"
            type="password"
            placeholder={t('profile.currentPassword')}
            autoComplete="current-password"
            value={currentPassword}
            onChange={(e) => setCurrentPassword(e.target.value)}
          />
          <input
            className="lx-input"
            type="password"
            placeholder={t('profile.newPassword')}
            autoComplete="new-password"
            value={newPassword}
            onChange={(e) => setNewPassword(e.target.value)}
          />
          <input
            className="lx-input"
            type="password"
            placeholder={t('profile.repeatPassword')}
            autoComplete="new-password"
            value={confirmPassword}
            onChange={(e) => setConfirmPassword(e.target.value)}
          />
          <button
            className="lx-btn-primary"
            style={{ alignSelf: 'flex-start' }}
            onClick={() => void handleChangePassword()}
            disabled={
              changePassword.isPending || !currentPassword || !newPassword || !confirmPassword
            }
          >
            {t('profile.changePassword')}
          </button>
        </div>
      </SectionCard>
    </>
  )
}
