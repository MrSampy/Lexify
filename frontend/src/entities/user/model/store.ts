import { create } from 'zustand'
import { setAuthHandlers, apiClient } from '@/shared/api'
import { type User, type AuthResponse, userFromJwt } from './types'

// The refresh token lives in an HttpOnly cookie managed by the backend — JS never sees it.
// Only the short-lived access token is kept here, in memory.

interface AuthStore {
  user: User | null
  accessToken: string | null
  isAuthenticated: boolean
  /** Set while an admin is acting as another user; holds the admin's own session to restore. */
  impersonation: { originalToken: string; originalUser: User } | null

  setAuth: (response: AuthResponse) => void
  logout: () => void
  refreshToken: () => Promise<boolean>
  startImpersonation: (token: string) => void
  stopImpersonation: () => void
}

export const useAuthStore = create<AuthStore>()((set, get) => ({
  user: null,
  accessToken: null,
  isAuthenticated: false,
  impersonation: null,

  setAuth: (response: AuthResponse) => {
    const user = userFromJwt(response.accessToken)
    set({ user, accessToken: response.accessToken, isAuthenticated: true })
  },

  logout: () => {
    set({ user: null, accessToken: null, isAuthenticated: false, impersonation: null })
  },

  refreshToken: async () => {
    try {
      // Refresh token cookie is attached automatically (withCredentials)
      const { data } = await apiClient.post<AuthResponse>('/api/auth/refresh')
      const user = userFromJwt(data.accessToken)
      // The refresh cookie belongs to the real admin, so a refresh always ends impersonation.
      set({ user, accessToken: data.accessToken, isAuthenticated: true, impersonation: null })
      return true
    } catch {
      set({ user: null, accessToken: null, isAuthenticated: false, impersonation: null })
      return false
    }
  },

  startImpersonation: (token: string) => {
    const { user, accessToken } = get()
    if (!user || !accessToken) return
    set({
      impersonation: { originalToken: accessToken, originalUser: user },
      user: userFromJwt(token),
      accessToken: token,
    })
  },

  stopImpersonation: () => {
    const { impersonation } = get()
    if (!impersonation) return
    set({
      user: impersonation.originalUser,
      accessToken: impersonation.originalToken,
      impersonation: null,
    })
  },
}))

// Wire apiClient interceptors — valid FSD: entities → shared
setAuthHandlers({
  getToken: () => useAuthStore.getState().accessToken,
  refresh: () => useAuthStore.getState().refreshToken(),
  logout: () => useAuthStore.getState().logout(),
})
