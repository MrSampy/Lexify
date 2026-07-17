import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useTheme } from 'next-themes'
import { toast } from 'sonner'
import {
  useProfile,
  useUpdateDisplayNameMutation,
  useChangePasswordMutation,
  useUpdateEnglishLevelMutation,
  useUpdateReviewSettingsMutation,
  ENGLISH_LEVELS,
  type EnglishLevel,
} from '@/entities/user'
import { LxSelect, Spinner } from '@/shared/ui'

const THEMES = [
  { value: 'light', labelKey: 'profile.themeLight' },
  { value: 'dark', labelKey: 'profile.themeDark' },
  { value: 'system', labelKey: 'profile.themeSystem' },
] as const

function SectionCard({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <section
      style={{
        background: 'var(--bg-1)',
        border: '1.5px solid var(--line-2)',
        borderRadius: 'var(--r-md)',
        padding: '20px 24px',
      }}
    >
      <h2
        style={{
          margin: '0 0 14px',
          fontFamily: 'var(--font-body)',
          fontWeight: 700,
          fontSize: 13,
          textTransform: 'uppercase',
          letterSpacing: '0.06em',
          color: 'var(--fg-2)',
        }}
      >
        {title}
      </h2>
      {children}
    </section>
  )
}

export function ProfilePage() {
  const { t } = useTranslation()
  const { data: profile, isLoading } = useProfile()
  const { theme, setTheme } = useTheme()

  const updateDisplayName = useUpdateDisplayNameMutation()
  const updateLevel = useUpdateEnglishLevelMutation()
  const changePassword = useChangePasswordMutation()
  const updateReviewSettings = useUpdateReviewSettingsMutation()

  // null = untouched by the user → show the profile value; avoids setState-in-effect
  const [displayNameDraft, setDisplayNameDraft] = useState<string | null>(null)
  const displayName = displayNameDraft ?? profile?.displayName ?? ''
  const [newWordsDraft, setNewWordsDraft] = useState<string | null>(null)
  const newWordsPerDay = newWordsDraft ?? String(profile?.newWordsPerDay ?? 10)
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')

  if (isLoading) {
    return (
      <div style={{ display: 'flex', justifyContent: 'center', padding: 80 }}>
        <Spinner size="lg" />
      </div>
    )
  }

  const handleSaveDisplayName = async () => {
    try {
      await updateDisplayName.mutateAsync(displayName.trim() === '' ? null : displayName.trim())
      setDisplayNameDraft(null)
      toast.success(t('profile.nameUpdated'))
    } catch {
      toast.error(t('profile.nameUpdateFailed'))
    }
  }

  const handleLevelChange = async (value: string) => {
    try {
      await updateLevel.mutateAsync(value === '' ? null : (value as EnglishLevel))
      toast.success(t('profile.levelUpdated'))
    } catch {
      toast.error(t('profile.levelUpdateFailed'))
    }
  }

  const handleSaveReviewSettings = async () => {
    const parsed = Number(newWordsPerDay)
    if (!Number.isInteger(parsed) || parsed < 0 || parsed > 100) {
      toast.error(t('profile.newWordsInvalid'))
      return
    }
    try {
      await updateReviewSettings.mutateAsync(parsed)
      setNewWordsDraft(null)
      toast.success(t('profile.reviewSettingsUpdated'))
    } catch {
      toast.error(t('profile.reviewSettingsUpdateFailed'))
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
    <div>
      <h1 className="ds-h2" style={{ margin: '0 0 6px' }}>
        {t('profile.title')}
      </h1>
      <p className="ds-body" style={{ margin: '0 0 24px', color: 'var(--fg-3)' }}>
        {profile?.email}
      </p>

      <div style={{ display: 'flex', flexDirection: 'column', gap: 20, maxWidth: 560 }}>
        <SectionCard title={t('profile.displayName')}>
          <div style={{ display: 'flex', gap: 10 }}>
            <input
              className="lx-input"
              style={{ flex: 1 }}
              value={displayName}
              placeholder={t('profile.namePlaceholder')}
              maxLength={64}
              onChange={(e) => setDisplayNameDraft(e.target.value)}
            />
            <button
              className="lx-btn-primary"
              onClick={() => void handleSaveDisplayName()}
              disabled={
                updateDisplayName.isPending || (profile?.displayName ?? '') === displayName.trim()
              }
            >
              {t('common.save')}
            </button>
          </div>
        </SectionCard>

        <SectionCard title={t('profile.englishLevel')}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
            <LxSelect
              value={profile?.englishLevel ?? ''}
              disabled={updateLevel.isPending}
              onValueChange={(v) => void handleLevelChange(v)}
              triggerStyle={{ width: '100%', maxWidth: 160 }}
              options={[
                { value: '', label: t('common.notSet') },
                ...ENGLISH_LEVELS.map((level) => ({ value: level, label: level })),
              ]}
            />
            <span className="ds-sm" style={{ color: 'var(--fg-4)' }}>
              {t('profile.cefrHint')}
            </span>
          </div>
        </SectionCard>

        <SectionCard title={t('profile.reviewSettings')}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 12, flexWrap: 'wrap' }}>
            <input
              className="lx-input"
              type="number"
              min={0}
              max={100}
              style={{ width: 100 }}
              value={newWordsPerDay}
              onChange={(e) => setNewWordsDraft(e.target.value)}
            />
            <button
              className="lx-btn-primary"
              onClick={() => void handleSaveReviewSettings()}
              disabled={
                updateReviewSettings.isPending ||
                String(profile?.newWordsPerDay ?? 10) === newWordsPerDay.trim()
              }
            >
              {t('common.save')}
            </button>
            <span className="ds-sm" style={{ color: 'var(--fg-4)' }}>
              {t('profile.newWordsHint')}
            </span>
          </div>
        </SectionCard>

        <SectionCard title={t('profile.theme')}>
          <div style={{ display: 'flex', gap: 8 }}>
            {THEMES.map(({ value, labelKey }) => (
              <button
                key={value}
                className={theme === value ? 'lx-btn-primary' : 'lx-btn-secondary'}
                onClick={() => setTheme(value)}
              >
                {t(labelKey)}
              </button>
            ))}
          </div>
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
      </div>
    </div>
  )
}
