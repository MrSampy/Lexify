import { useTranslation } from 'react-i18next'
import {
  useProfile,
  useUpdateEnglishLevelMutation,
  ENGLISH_LEVELS,
  type EnglishLevel,
} from '@/entities/user'

/**
 * CEFR level selector — persisted on the user profile so test generation
 * can match question difficulty to the learner's level.
 */
export function EnglishLevelSelect() {
  const { t } = useTranslation()
  const { data: profile, isLoading } = useProfile()
  const updateLevel = useUpdateEnglishLevelMutation()

  const handleChange = (value: string) => {
    updateLevel.mutate(value === '' ? null : (value as EnglishLevel))
  }

  return (
    <div style={{ marginTop: 16 }}>
      <label className="lx-label" style={{ marginBottom: 6, display: 'block' }}>
        {t('genTest.yourLevel')}
      </label>
      <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
        <select
          className="lx-input"
          style={{ width: 160 }}
          value={profile?.englishLevel ?? ''}
          disabled={isLoading || updateLevel.isPending}
          onChange={(e) => handleChange(e.target.value)}
        >
          <option value="">{t('common.notSet')}</option>
          {ENGLISH_LEVELS.map((level) => (
            <option key={level} value={level}>
              {level}
            </option>
          ))}
        </select>
        <span className="ds-sm" style={{ color: 'var(--fg-4)' }}>
          {t('genTest.cefrHint')}
        </span>
      </div>
    </div>
  )
}
