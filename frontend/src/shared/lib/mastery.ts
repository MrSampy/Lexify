/**
 * SM-2 maturity buckets, mirroring the backend GetMasteryCounts thresholds. Shared by the stats page
 * (distribution chart) and the word row (per-word badge) so both label a word's progress identically.
 */
export type MasteryLevel = 'new' | 'learning' | 'young' | 'mature'

export interface MasteryInfo {
  level: MasteryLevel
  /** i18n key for the human label, e.g. 'stats.masteryNew'. */
  labelKey: string
  /** CSS variable expression for the bucket's accent colour. */
  color: string
}

const INFO: Record<MasteryLevel, MasteryInfo> = {
  new: { level: 'new', labelKey: 'stats.masteryNew', color: 'var(--fg-4)' },
  learning: { level: 'learning', labelKey: 'stats.masteryLearning', color: 'var(--warning)' },
  young: { level: 'young', labelKey: 'stats.masteryYoung', color: 'var(--blue)' },
  mature: { level: 'mature', labelKey: 'stats.masteryMature', color: 'var(--success)' },
}

export function masteryLevel(repetitions: number, intervalDays: number): MasteryLevel {
  if (repetitions <= 0) return 'new'
  if (intervalDays < 7) return 'learning'
  if (intervalDays <= 30) return 'young'
  return 'mature'
}

export function masteryInfo(repetitions: number, intervalDays: number): MasteryInfo {
  return INFO[masteryLevel(repetitions, intervalDays)]
}

export function masteryInfoFor(level: MasteryLevel): MasteryInfo {
  return INFO[level]
}
