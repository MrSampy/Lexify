import { apiClient } from '@/shared/api'
import type { AuthResponse } from '@/entities/user'

/**
 * `open` — anyone may sign up. `inviteRequired` — sign-up is closed but a valid invite code still
 * gets a user in. Both false means sign-up is shut outright. `emailVerificationRequired` — when false
 * the account is auto-confirmed, so the form goes straight to login instead of the check-email screen.
 */
export interface RegistrationStatus {
  open: boolean
  inviteRequired: boolean
  emailVerificationRequired: boolean
}

/** Sign-in step 1 returned a 2FA challenge instead of a session; the code must be entered next. */
export interface TwoFactorChallenge {
  twoFactorRequired: true
  challengeToken: string
}

export type LoginResult = AuthResponse | TwoFactorChallenge

export function isTwoFactorChallenge(result: LoginResult): result is TwoFactorChallenge {
  return (result as TwoFactorChallenge).twoFactorRequired === true
}

export const authApi = {
  login: (email: string, password: string) =>
    apiClient.post<LoginResult>('/api/auth/login', { email, password }).then((r) => r.data),

  // Step 2 of sign-in: exchange the challenge token + emailed code for a real session.
  verifyTwoFactor: (challengeToken: string, code: string) =>
    apiClient
      .post<AuthResponse>('/api/auth/login/verify-2fa', { challengeToken, code })
      .then((r) => r.data),

  resendTwoFactorCode: (challengeToken: string) =>
    apiClient.post('/api/auth/login/resend-2fa', { challengeToken }).then((r) => r.data),

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

  verifyEmail: (token: string) =>
    apiClient.post<VerifyEmailResult>('/api/auth/verify-email', { token }).then((r) => r.data),

  // Always resolves 200 regardless of whether the address exists or is already confirmed
  resendVerification: (email: string) =>
    apiClient.post('/api/auth/resend-verification', { email }).then((r) => r.data),
}

export interface VerifyEmailResult {
  email: string
  /** True when the link completed an address change rather than a sign-up. */
  emailChanged: boolean
}

/** Login was refused only because the address is unconfirmed — the client offers a resend instead. */
export const EMAIL_NOT_VERIFIED = 'email_not_verified'

/** Marker paralleling EMAIL_NOT_VERIFIED; the 2FA case rides on a success body, not an error. */
export const TWO_FACTOR_REQUIRED = 'two_factor_required'
