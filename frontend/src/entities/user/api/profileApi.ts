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
  emailVerified: boolean
  /** Address awaiting confirmation from an in-flight change; null when none. */
  pendingEmail: string | null
  /** The user's 2FA opt-in flag. */
  twoFactorEnabled: boolean
  /** True for admins — 2FA is forced on and cannot be turned off. */
  twoFactorMandatory: boolean
  /** False when the user opted out of the daily "words are due" email. */
  emailRemindersEnabled: boolean
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

/** Turns the daily reminder email on or off. */
export function useUpdateNotificationSettingsMutation() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (emailRemindersEnabled: boolean) =>
      apiClient.put('/api/profile/notifications', { emailRemindersEnabled }).then((r) => r.data),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ['user', 'profile'] }),
  })
}

export function useChangePasswordMutation() {
  return useMutation({
    mutationFn: (input: { currentPassword: string; newPassword: string }) =>
      apiClient.put('/api/profile/password', input).then((r) => r.data),
  })
}

/** Starts an email change; the account keeps its current address until the link is confirmed. */
export function useRequestEmailChangeMutation() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (input: { newEmail: string; currentPassword: string }) =>
      apiClient.put('/api/profile/email', input).then((r) => r.data),
    // Refetch so the "waiting on confirmation" hint appears.
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ['user', 'profile'] }),
  })
}

/** Step 1 of opting into 2FA: request the enrollment code (does not flip the flag yet). */
export function useEnableTwoFactorMutation() {
  return useMutation({
    mutationFn: () => apiClient.post('/api/profile/2fa/enable').then((r) => r.data),
  })
}

/** Re-sends the enrollment code while opting in. */
export function useResendEnableTwoFactorMutation() {
  return useMutation({
    mutationFn: () => apiClient.post('/api/profile/2fa/resend').then((r) => r.data),
  })
}

/** Step 2 of opting in: confirm the emailed code, which turns 2FA on. */
export function useConfirmTwoFactorMutation() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (code: string) =>
      apiClient.post('/api/profile/2fa/confirm', { code }).then((r) => r.data),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ['user', 'profile'] }),
  })
}

/** Turns 2FA off (re-authenticated by password). Rejected for admins. */
export function useDisableTwoFactorMutation() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (currentPassword: string) =>
      apiClient.delete('/api/profile/2fa', { data: { currentPassword } }).then((r) => r.data),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ['user', 'profile'] }),
  })
}
