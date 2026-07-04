export type UserRole = 'user' | 'moderator' | 'admin'

export interface User {
  id: string
  email: string
  displayName: string | null
  role: UserRole
}

// Refresh token is delivered via HttpOnly cookie, never in the response body
export interface AuthResponse {
  accessToken: string
  expiresAt: string
}

const DOTNET_ROLE_CLAIM = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'

// eslint-disable-next-line @typescript-eslint/no-explicit-any
type JwtPayload = Record<string, any>

export function parseJwt(token: string): JwtPayload {
  const payload = token.split('.')[1]
  return JSON.parse(atob(payload.replace(/-/g, '+').replace(/_/g, '/'))) as JwtPayload
}

export function userFromJwt(token: string): User {
  const payload = parseJwt(token)
  const role = (payload[DOTNET_ROLE_CLAIM] ?? payload['role'] ?? 'user') as UserRole
  return { id: payload.sub, email: payload.email, role, displayName: null }
}
