import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '@/shared/api'
import type { PagedResult } from '@/shared/api'
import type {
  AdminUser,
  AdminUsersParams,
  AiCallDataPoint,
  AiLog,
  AiLogsParams,
  AiProviderStatus,
  AiStats,
  AddLanguageInput,
  DashboardStats,
  Language,
  RegistrationDataPoint,
  SystemSetting,
} from '../model/types'

export const adminKeys = {
  dashboardStats: () => ['admin', 'stats'] as const,
  registrationsChart: (days: number) => ['admin', 'chart', 'registrations', days] as const,
  aiCallsChart: (hours: number) => ['admin', 'chart', 'ai-calls', hours] as const,
  users: (params: AdminUsersParams) => ['admin', 'users', params] as const,
  aiLogs: (params: AiLogsParams) => ['admin', 'ai', 'logs', params] as const,
  aiStats: (hours: number) => ['admin', 'ai', 'stats', hours] as const,
  aiStatus: () => ['admin', 'ai', 'status'] as const,
  settings: () => ['admin', 'settings'] as const,
  languages: () => ['admin', 'languages'] as const,
}

const adminApi = {
  getDashboardStats: () => apiClient.get<DashboardStats>('/api/admin/stats').then((r) => r.data),

  getRegistrationsChart: (days: number) =>
    apiClient
      .get<RegistrationDataPoint[]>('/api/admin/charts/registrations', { params: { days } })
      .then((r) => r.data),

  getAiCallsChart: (hours: number) =>
    apiClient
      .get<AiCallDataPoint[]>('/api/admin/charts/ai-calls', { params: { hours } })
      .then((r) => r.data),

  getUsers: (params: AdminUsersParams) =>
    apiClient.get<PagedResult<AdminUser>>('/api/admin/users', { params }).then((r) => r.data),

  suspendUser: (id: string) => apiClient.put(`/api/admin/users/${id}/suspend`).then((r) => r.data),

  restoreUser: (id: string) => apiClient.put(`/api/admin/users/${id}/restore`).then((r) => r.data),

  deleteUser: (id: string) => apiClient.delete(`/api/admin/users/${id}`).then((r) => r.data),

  changeUserRole: ({ id, role }: { id: string; role: string }) =>
    apiClient.put(`/api/admin/users/${id}/role`, { role }).then((r) => r.data),

  getAiLogs: (params: AiLogsParams) =>
    apiClient.get<PagedResult<AiLog>>('/api/admin/ai/logs', { params }).then((r) => r.data),

  getAiStats: (hours: number) =>
    apiClient.get<AiStats>('/api/admin/ai/stats', { params: { hours } }).then((r) => r.data),

  getAiStatus: () => apiClient.get<AiProviderStatus[]>('/api/admin/ai/status').then((r) => r.data),

  getSettings: () => apiClient.get<SystemSetting[]>('/api/admin/settings').then((r) => r.data),

  updateSetting: ({ key, value }: { key: string; value: string }) =>
    apiClient.put(`/api/admin/settings/${key}`, { value }).then((r) => r.data),

  getLanguages: () => apiClient.get<Language[]>('/api/admin/languages').then((r) => r.data),

  addLanguage: (input: AddLanguageInput) =>
    apiClient.post<Language>('/api/admin/languages', input).then((r) => r.data),

  toggleLanguage: (code: string) =>
    apiClient.put(`/api/admin/languages/${code}/toggle`).then((r) => r.data),
}

// ---- Queries ----

export function useDashboardStats() {
  return useQuery({
    queryKey: adminKeys.dashboardStats(),
    queryFn: adminApi.getDashboardStats,
    staleTime: 5 * 60 * 1000,
  })
}

export function useRegistrationsChart(days = 30) {
  return useQuery({
    queryKey: adminKeys.registrationsChart(days),
    queryFn: () => adminApi.getRegistrationsChart(days),
    staleTime: 5 * 60 * 1000,
  })
}

export function useAiCallsChart(hours = 24) {
  return useQuery({
    queryKey: adminKeys.aiCallsChart(hours),
    queryFn: () => adminApi.getAiCallsChart(hours),
    staleTime: 5 * 60 * 1000,
  })
}

export function useAdminUsers(params: AdminUsersParams) {
  return useQuery({
    queryKey: adminKeys.users(params),
    queryFn: () => adminApi.getUsers(params),
    staleTime: 2 * 60 * 1000,
  })
}

export function useAiLogs(params: AiLogsParams) {
  return useQuery({
    queryKey: adminKeys.aiLogs(params),
    queryFn: () => adminApi.getAiLogs(params),
    staleTime: 2 * 60 * 1000,
  })
}

export function useAiStats(hours = 24) {
  return useQuery({
    queryKey: adminKeys.aiStats(hours),
    queryFn: () => adminApi.getAiStats(hours),
    staleTime: 5 * 60 * 1000,
  })
}

export function useAiStatus() {
  return useQuery({
    queryKey: adminKeys.aiStatus(),
    queryFn: adminApi.getAiStatus,
    staleTime: 2 * 60 * 1000,
  })
}

export function useSettings() {
  return useQuery({
    queryKey: adminKeys.settings(),
    queryFn: adminApi.getSettings,
    staleTime: 5 * 60 * 1000,
  })
}

export function useLanguages() {
  return useQuery({
    queryKey: adminKeys.languages(),
    queryFn: adminApi.getLanguages,
    staleTime: 5 * 60 * 1000,
  })
}

// ---- Mutations ----

export function useSuspendUserMutation() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: adminApi.suspendUser,
    onSuccess: () => void qc.invalidateQueries({ queryKey: ['admin', 'users'] }),
  })
}

export function useRestoreUserMutation() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: adminApi.restoreUser,
    onSuccess: () => void qc.invalidateQueries({ queryKey: ['admin', 'users'] }),
  })
}

export function useDeleteUserMutation() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: adminApi.deleteUser,
    onSuccess: () => void qc.invalidateQueries({ queryKey: ['admin', 'users'] }),
  })
}

export function useChangeRoleMutation() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: adminApi.changeUserRole,
    onSuccess: () => void qc.invalidateQueries({ queryKey: ['admin', 'users'] }),
  })
}

export function useUpdateSettingMutation() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: adminApi.updateSetting,
    onSuccess: () => void qc.invalidateQueries({ queryKey: adminKeys.settings() }),
  })
}

export function useAddLanguageMutation() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: adminApi.addLanguage,
    onSuccess: () => void qc.invalidateQueries({ queryKey: adminKeys.languages() }),
  })
}

export function useToggleLanguageMutation() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: adminApi.toggleLanguage,
    onSuccess: () => void qc.invalidateQueries({ queryKey: adminKeys.languages() }),
  })
}
