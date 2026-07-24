import axios from 'axios'
import { env } from '@/shared/config/env'

export type AuthHandlers = {
  getToken: () => string | null
  refresh: () => Promise<boolean>
  logout: () => void
}

let authHandlers: AuthHandlers | null = null

export function setAuthHandlers(handlers: AuthHandlers): void {
  authHandlers = handlers
}

/** For non-axios transports (e.g. SSE fetch) that need the same injected auth. */
export function getAuthHandlers(): AuthHandlers | null {
  return authHandlers
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

  // File uploads send FormData: drop the JSON default so axios can set multipart/form-data
  // *with* its generated boundary — without it the server rejects the body as 415.
  if (config.data instanceof FormData) {
    delete config.headers['Content-Type']
  }

  return config
})

// Auto-refresh on 401 and retry the original request once.
// Concurrent 401s are safe: `authHandlers.refresh` is single-flighted in entities/user's store,
// so N simultaneous failures share one POST /api/auth/refresh instead of racing the server-side
// token rotation (which revokes the old token and would strand every loser).
apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config as typeof error.config & { _retry?: boolean }

    // The refresh call itself must never trigger a refresh — that would recurse.
    if (originalRequest?.url?.includes('/api/auth/refresh')) {
      return Promise.reject(error)
    }

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
