export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
  hasNextPage: boolean
  hasPreviousPage: boolean
}

export interface ApiError {
  status: number
  title: string
  detail?: string
  errors?: Record<string, string[]>
}

export type ResultStatus = 'Ok' | 'NotFound' | 'Forbidden' | 'Failure'

export interface Result<T> {
  status: ResultStatus
  value?: T
  error?: string
}
