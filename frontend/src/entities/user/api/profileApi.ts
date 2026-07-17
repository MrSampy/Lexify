import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '@/shared/api'

export const ENGLISH_LEVELS = ['A1', 'A2', 'B1', 'B2', 'C1', 'C2'] as const
export type EnglishLevel = (typeof ENGLISH_LEVELS)[number]

export interface Profile {
  email: string
  displayName: string | null
  englishLevel: EnglishLevel | null
  /** Max new (never-reviewed) words introduced into the review queue per day. */
  newWordsPerDay: number
}

export function useProfile() {
  return useQuery({
    queryKey: ['user', 'profile'],
    queryFn: () => apiClient.get<Profile>('/api/profile').then((r) => r.data),
    staleTime: 60_000,
  })
}

export function useUpdateEnglishLevelMutation() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (englishLevel: EnglishLevel | null) =>
      apiClient.put('/api/profile/english-level', { englishLevel }).then((r) => r.data),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ['user', 'profile'] }),
  })
}

export function useUpdateDisplayNameMutation() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (displayName: string | null) =>
      apiClient.put('/api/profile/display-name', { displayName }).then((r) => r.data),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ['user', 'profile'] }),
  })
}

export function useUpdateReviewSettingsMutation() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (newWordsPerDay: number) =>
      apiClient.put('/api/profile/review-settings', { newWordsPerDay }).then((r) => r.data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['user', 'profile'] })
      // The setting changes what "due" means for the dashboard counter and review queue.
      void queryClient.invalidateQueries({ queryKey: ['user', 'stats'] })
      void queryClient.invalidateQueries({ queryKey: ['review'] })
    },
  })
}

export function useChangePasswordMutation() {
  return useMutation({
    mutationFn: (input: { currentPassword: string; newPassword: string }) =>
      apiClient.put('/api/profile/password', input).then((r) => r.data),
  })
}
