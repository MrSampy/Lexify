import { apiClient } from '@/shared/api'
import type { AuthResponse } from '@/entities/user'

export const authApi = {
  login: (email: string, password: string) =>
    apiClient.post<AuthResponse>('/api/auth/login', { email, password }).then((r) => r.data),

  register: (email: string, password: string, displayName?: string) =>
    apiClient
      .post<string>('/api/auth/register', { email, password, displayName })
      .then((r) => r.data),

  logout: (token: string) => apiClient.post('/api/auth/logout', { token }).then((r) => r.data),

  refresh: (token: string) =>
    apiClient.post<AuthResponse>('/api/auth/refresh', { token }).then((r) => r.data),
}
