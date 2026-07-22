import { useQuery } from '@tanstack/react-query'
import { apiClient } from '@/shared/api'

export interface UserStats {
  totalBlocks: number
  totalWords: number
  dueWordsCount: number
  /** New (never-reviewed) words that fit into today's remaining new-word budget. */
  dueNewCount: number
  /** Previously reviewed words whose next review is due. */
  dueReviewCount: number
  wordsAnsweredThisWeek: number
  testsCompletedThisWeek: number
  currentStreak: number
}

export function useUserStats() {
  return useQuery({
    queryKey: ['user', 'stats'],
    queryFn: () => apiClient.get<UserStats>('/api/stats').then((r) => r.data),
    staleTime: 60_000,
  })
}

export interface DailyReviewCount {
  date: string // ISO date (yyyy-MM-dd)
  count: number
}

export interface ActivityStats {
  days: DailyReviewCount[]
  currentStreak: number
  longestStreak: number
  totalReviews: number
}

export function useActivityStats(days = 90) {
  return useQuery({
    queryKey: ['user', 'stats', 'activity', days],
    queryFn: () =>
      apiClient.get<ActivityStats>('/api/stats/activity', { params: { days } }).then((r) => r.data),
    staleTime: 60_000,
  })
}

export interface MasteryStats {
  new: number
  learning: number
  young: number
  mature: number
}

export function useMasteryStats() {
  return useQuery({
    queryKey: ['user', 'stats', 'mastery'],
    queryFn: () => apiClient.get<MasteryStats>('/api/stats/mastery').then((r) => r.data),
    staleTime: 60_000,
  })
}

export interface DailyDueCount {
  date: string // ISO date (yyyy-MM-dd)
  count: number
}

export interface ForecastStats {
  days: DailyDueCount[]
}

export function useForecastStats(days = 14) {
  return useQuery({
    queryKey: ['user', 'stats', 'forecast', days],
    queryFn: () =>
      apiClient.get<ForecastStats>('/api/stats/forecast', { params: { days } }).then((r) => r.data),
    staleTime: 60_000,
  })
}

export interface ProblemWord {
  wordId: string
  blockId: string
  blockTitle: string
  term: string
  translation: string
  lapseCount: number
  easeFactor: number
  intervalDays: number
  nextReviewAt: string
  confidenceFlag: boolean
}

export function useProblemWords(limit = 20) {
  return useQuery({
    queryKey: ['user', 'stats', 'problem-words', limit],
    queryFn: () =>
      apiClient
        .get<ProblemWord[]>('/api/stats/problem-words', { params: { limit } })
        .then((r) => r.data),
    staleTime: 60_000,
  })
}

export interface ConversationPracticeStats {
  totalSessions: number
  wordsPractised: number
  /** Average stars over ended sessions; null until at least one scored session exists. */
  avgStars: number | null
}

export function useConversationStats() {
  return useQuery({
    queryKey: ['user', 'stats', 'conversations'],
    queryFn: () =>
      apiClient.get<ConversationPracticeStats>('/api/stats/conversations').then((r) => r.data),
    staleTime: 60_000,
  })
}

export interface DailyAccuracy {
  date: string
  total: number
  correct: number
}

export interface AccuracyStats {
  days: DailyAccuracy[]
}

export function useAccuracyStats(days = 30) {
  return useQuery({
    queryKey: ['user', 'stats', 'accuracy', days],
    queryFn: () =>
      apiClient.get<AccuracyStats>('/api/stats/accuracy', { params: { days } }).then((r) => r.data),
    staleTime: 60_000,
  })
}
