import { apiClient } from '@/shared/api'
import type { AuthResponse } from '@/entities/user'

/**
 * `open` — anyone may sign up. `inviteRequired` — sign-up is closed but a valid invite code still
 * gets a user in. Both false means sign-up is shut outright.
 */
export interface RegistrationStatus {
  open: boolean
  inviteRequired: boolean
}

export const authApi = {
  login: (email: string, password: string) =>
    apiClient.post<AuthResponse>('/api/auth/login', { email, password }).then((r) => r.data),

  register: (email: string, password: string, displayName?: string, inviteCode?: string) =>
    apiClient
      .post<string>('/api/auth/register', { email, password, displayName, inviteCode })
      .then((r) => r.data),

  registrationStatus: () =>
    apiClient.get<RegistrationStatus>('/api/auth/registration-status').then((r) => r.data),

  // Refresh token travels in an HttpOnly cookie — no body needed
  logout: () => apiClient.post('/api/auth/logout').then((r) => r.data),

  refresh: () => apiClient.post<AuthResponse>('/api/auth/refresh').then((r) => r.data),

  // Always resolves 200 regardless of whether the email exists (anti-enumeration)
  forgotPassword: (email: string) =>
    apiClient.post('/api/auth/forgot-password', { email }).then((r) => r.data),

  resetPassword: (token: string, newPassword: string) =>
    apiClient.post('/api/auth/reset-password', { token, newPassword }).then((r) => r.data),
}
