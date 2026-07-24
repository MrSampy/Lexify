import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import {
  useUpdateEnglishLevelMutation,
  useUpdateReviewSettingsMutation,
  useUpdateNotificationSettingsMutation,
  ENGLISH_LEVELS,
  type EnglishLevel,
  type Profile,
} from '@/entities/user'
import { LxSelect } from '@/shared/ui'
import { SectionCard } from './SectionCard'

/** Everything that shapes the study loop: level, how many new words per day, and the daily email. */
export function LearningTab({ profile }: { profile: Profile }) {
  const { t } = useTranslation()

  const updateLevel = useUpdateEnglishLevelMutation()
  const updateReviewSettings = useUpdateReviewSettingsMutation()
  const updateNotifications = useUpdateNotificationSettingsMutation()

  const [newWordsDraft, setNewWordsDraft] = useState<string | null>(null)
  const newWordsPerDay = newWordsDraft ?? String(profile.newWordsPerDay)

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

  const handleToggleReminders = async () => {
    try {
      await updateNotifications.mutateAsync(!profile.emailRemindersEnabled)
      toast.success(t('profile.emailRemindersUpdated'))
    } catch {
      toast.error(t('profile.emailRemindersUpdateFailed'))
    }
  }

  return (
    <>
      <SectionCard title={t('profile.englishLevel')}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 12, flexWrap: 'wrap' }}>
          <LxSelect
            value={profile.englishLevel ?? ''}
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
              String(profile.newWordsPerDay) === newWordsPerDay.trim()
            }
          >
            {t('common.save')}
          </button>
          <span className="ds-sm" style={{ color: 'var(--fg-4)' }}>
            {t('profile.newWordsHint')}
          </span>
        </div>
      </SectionCard>

      <SectionCard title={t('profile.notifications')}>
        <div
          style={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            gap: 12,
            flexWrap: 'wrap',
          }}
        >
          <div style={{ flex: '1 1 220px' }}>
            <div className="ds-sm" style={{ color: 'var(--fg-1)', fontWeight: 700 }}>
              {t('profile.emailReminders')}
            </div>
            <div className="ds-sm" style={{ color: 'var(--fg-3)' }}>
              {profile.emailRemindersEnabled
                ? t('profile.emailRemindersOn')
                : t('profile.emailRemindersOff')}
            </div>
          </div>
          <ToggleSwitch
            checked={profile.emailRemindersEnabled}
            disabled={updateNotifications.isPending}
            label={t('profile.emailReminders')}
            onToggle={() => void handleToggleReminders()}
          />
        </div>
      </SectionCard>
    </>
  )
}

interface ToggleSwitchProps {
  checked: boolean
  disabled?: boolean
  label: string
  onToggle: () => void
}

function ToggleSwitch({ checked, disabled, label, onToggle }: ToggleSwitchProps) {
  return (
    <button
      type="button"
      role="switch"
      aria-checked={checked}
      aria-label={label}
      disabled={disabled}
      onClick={onToggle}
      style={{
        flexShrink: 0,
        width: 52,
        height: 30,
        padding: 3,
        border: '1.5px solid var(--line-2)',
        borderRadius: 'var(--r-pill)',
        background: checked ? 'var(--accent-color)' : 'var(--bg-3)',
        cursor: disabled ? 'default' : 'pointer',
        opacity: disabled ? 0.6 : 1,
        transition: 'background 0.15s',
        display: 'flex',
        justifyContent: checked ? 'flex-end' : 'flex-start',
      }}
    >
      <span
        style={{
          width: 20,
          height: 20,
          borderRadius: '50%',
          background: '#fff',
          boxShadow: 'var(--shadow-1)',
          transition: 'all 0.15s',
        }}
      />
    </button>
  )
}
