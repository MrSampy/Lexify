import { useTranslation } from 'react-i18next'
import { LxSelect } from '@/shared/ui'
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
    <div>
      <label className="lx-label mb-1.5 block">{t('genTest.yourLevel')}</label>
      <div className="flex items-center gap-3">
        <LxSelect
          value={profile?.englishLevel ?? ''}
          disabled={isLoading || updateLevel.isPending}
          onValueChange={handleChange}
          triggerStyle={{ width: '100%', maxWidth: 160 }}
          options={[
            { value: '', label: t('common.notSet') },
            ...ENGLISH_LEVELS.map((level) => ({ value: level, label: level })),
          ]}
        />
        <span className="text-xs font-semibold text-[var(--fg-4)]">{t('genTest.cefrHint')}</span>
      </div>
    </div>
  )
}
