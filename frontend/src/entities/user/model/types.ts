export type UserRole = 'user' | 'moderator' | 'admin'

export interface User {
  id: string
  email: string
  displayName: string | null
  role: UserRole
}

export interface AuthResponse {
  accessToken: string
  refreshToken: string
  expiresAt: string
}

interface JwtPayload {
  sub: string
  email: string
  role: UserRole
  exp: number
}

export function parseJwt(token: string): JwtPayload {
  const payload = token.split('.')[1]
  return JSON.parse(atob(payload.replace(/-/g, '+').replace(/_/g, '/'))) as JwtPayload
}

export function userFromJwt(token: string): User {
  const payload = parseJwt(token)
  return { id: payload.sub, email: payload.email, role: payload.role, displayName: null }
}
