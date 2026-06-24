import { create } from 'zustand'
import { setAuthHandlers, apiClient } from '@/shared/api'
import { type User, type AuthResponse, userFromJwt } from './types'

const RT_KEY = 'lexify_rt'

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
    sessionStorage.setItem(RT_KEY, response.refreshToken)
    set({ user, accessToken: response.accessToken, isAuthenticated: true })
  },

  logout: () => {
    sessionStorage.removeItem(RT_KEY)
    set({ user: null, accessToken: null, isAuthenticated: false })
  },

  refreshToken: async () => {
    const token = sessionStorage.getItem(RT_KEY)
    if (!token) return false
    try {
      const { data } = await apiClient.post<AuthResponse>('/api/auth/refresh', { token })
      const user = userFromJwt(data.accessToken)
      sessionStorage.setItem(RT_KEY, data.refreshToken)
      set({ user, accessToken: data.accessToken, isAuthenticated: true })
      return true
    } catch {
      sessionStorage.removeItem(RT_KEY)
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
