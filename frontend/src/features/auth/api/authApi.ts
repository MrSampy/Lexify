import { apiClient } from '@/shared/api'
import type { AuthResponse } from '@/entities/user'

export const authApi = {
  login: (email: string, password: string) =>
    apiClient.post<AuthResponse>('/api/auth/login', { email, password }).then((r) => r.data),

  register: (email: string, password: string, displayName?: string) =>
    apiClient
      .post<string>('/api/auth/register', { email, password, displayName })
      .then((r) => r.data),

  // Refresh token travels in an HttpOnly cookie — no body needed
  logout: () => apiClient.post('/api/auth/logout').then((r) => r.data),

  refresh: () => apiClient.post<AuthResponse>('/api/auth/refresh').then((r) => r.data),
}
