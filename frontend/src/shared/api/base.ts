import axios from 'axios'
import { env } from '@/shared/config/env'

type AuthHandlers = {
  getToken: () => string | null
  refresh: () => Promise<boolean>
  logout: () => void
}

let authHandlers: AuthHandlers | null = null

export function setAuthHandlers(handlers: AuthHandlers): void {
  authHandlers = handlers
}

export const apiClient = axios.create({
  baseURL: env.API_URL,
  timeout: env.API_TIMEOUT_MS,
  withCredentials: true,
  headers: { 'Content-Type': 'application/json' },
})

// Attach Bearer token from injected store
apiClient.interceptors.request.use((config) => {
  const token = authHandlers?.getToken()
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

// Auto-refresh on 401 and retry the original request once
apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config as typeof error.config & { _retry?: boolean }

    if (error.response?.status === 401 && !originalRequest._retry && authHandlers) {
      originalRequest._retry = true
      const refreshed = await authHandlers.refresh()
      if (refreshed) {
        const token = authHandlers.getToken()
        if (token) {
          originalRequest.headers.Authorization = `Bearer ${token}`
        }
        return apiClient.request(originalRequest)
      }
      authHandlers.logout()
    }

    return Promise.reject(error)
  },
)
