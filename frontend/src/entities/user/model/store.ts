import { create } from 'zustand'
import { setAuthHandlers, apiClient } from '@/shared/api'
import { type User, type AuthResponse, userFromJwt } from './types'

// The refresh token lives in an HttpOnly cookie managed by the backend — JS never sees it.
// Only the short-lived access token is kept here, in memory.

interface AuthStore {
  user: User | null
  accessToken: string | null
  isAuthenticated: boolean

  setAuth: (response: AuthResponse) => void
  logout: () => void
  refreshToken: () => Promise<boolean>
}

export const useAuthStore = create<AuthStore>()((set) => ({
  user: null,
  accessToken: null,
  isAuthenticated: false,

  setAuth: (response: AuthResponse) => {
    const user = userFromJwt(response.accessToken)
    set({ user, accessToken: response.accessToken, isAuthenticated: true })
  },

  logout: () => {
    set({ user: null, accessToken: null, isAuthenticated: false })
  },

  refreshToken: async () => {
    try {
      // Refresh token cookie is attached automatically (withCredentials)
      const { data } = await apiClient.post<AuthResponse>('/api/auth/refresh')
      const user = userFromJwt(data.accessToken)
      set({ user, accessToken: data.accessToken, isAuthenticated: true })
      return true
    } catch {
      set({ user: null, accessToken: null, isAuthenticated: false })
      return false
    }
  },
}))

// Wire apiClient interceptors — valid FSD: entities → shared
setAuthHandlers({
  getToken: () => useAuthStore.getState().accessToken,
  refresh: () => useAuthStore.getState().refreshToken(),
  logout: () => useAuthStore.getState().logout(),
})
