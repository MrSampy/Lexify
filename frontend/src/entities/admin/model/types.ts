export interface AdminUser {
  id: string
  email: string
  displayName: string | null
  role: string
  status: string
  lastActiveAt: string | null
  createdAt: string
  blockCount: number
  wordCount: number
  testCount: number
}

export interface DashboardStats {
  totalUsers: number
  activeUsersLast7Days: number
  activeUsersLast30Days: number
  totalWords: number
  totalWordBlocks: number
  totalTests: number
  aiCallsLast24Hours: number
  aiCallsLast7Days: number
  topLanguages: Array<{ code: string; name: string; blockCount: number }>
}

export interface RegistrationDataPoint {
  date: string
  count: number
}

export interface AiCallDataPoint {
  hourStart: string
  count: number
}

export interface AiLog {
  id: string
  userId: string | null
  callType: string
  provider: string
  model: string
  inputTokens: number | null
  outputTokens: number | null
  durationMs: number
  success: boolean
  errorMessage: string | null
  createdAt: string
}

export interface AiCallTypeStat {
  callType: string
  count: number
  avgDurationMs: number
  errorCount: number
}

export interface AiProviderStat {
  provider: string
  count: number
  avgDurationMs: number
  errorCount: number
}

export interface AiStats {
  totalCalls: number
  successfulCalls: number
  failedCalls: number
  errorRatePercent: number
  fallbackCount: number
  averageResponseTimeMs: number
  byCallType: AiCallTypeStat[]
  byProvider: AiProviderStat[]
}

export interface AiProviderStatus {
  provider: string
  status: string
  recentCallCount: number
  recentSuccessRatePercent: number
  lastCallAt: string | null
}

export interface SystemSetting {
  key: string
  value: string
  valueType: string
  description: string | null
  updatedAt: string
  updatedBy: string | null
}

export interface Language {
  id: number
  code: string
  name: string
  nativeName: string
  isActive: boolean
  sortOrder: number
}

export interface AddLanguageInput {
  code: string
  name: string
  nativeName: string
  sortOrder?: number
}

export interface AdminUsersParams {
  page: number
  pageSize: number
  role?: string
  status?: string
  email?: string
}

export interface SystemHealthCheck {
  name: string
  status: 'Healthy' | 'Degraded' | 'Unhealthy'
}

export interface SystemHealth {
  status: 'Healthy' | 'Degraded' | 'Unhealthy'
  checks: SystemHealthCheck[]
  /** Hangfire failed-job count; null when the job storage is unreachable. */
  failedJobs: number | null
  /** Newest file in the backup volume; null when none found or not monitored. */
  lastBackupAt: string | null
  /** False when the backup volume isn't mounted into the backend (e.g. local dev). */
  backupMonitored: boolean
}

export interface AuditLog {
  id: string
  adminId: string
  adminEmail: string | null
  action: string
  targetType: string | null
  targetId: string | null
  oldValue: string | null
  newValue: string | null
  ipAddress: string | null
  createdAt: string
}

export interface AuditLogsParams {
  page: number
  pageSize?: number
  action?: string
  adminId?: string
  dateFrom?: string
  dateTo?: string
}

export interface AiLogsParams {
  page: number
  pageSize?: number
  provider?: string
  callType?: string
  success?: boolean
  dateFrom?: string
  dateTo?: string
}
