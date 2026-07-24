import { apiClient } from '@/shared/api'

export const unsubscribeApi = {
  /** Opts the account behind the signed token out of daily reminder emails. Anonymous. */
  unsubscribe: (token: string) =>
    apiClient.post('/api/notifications/unsubscribe', { token }).then((r) => r.data),
}
