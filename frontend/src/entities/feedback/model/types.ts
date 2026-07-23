export type FeedbackType = 'suggestion' | 'bug' | 'review' | 'question'

export type FeedbackStatus = 'new' | 'in_progress' | 'resolved'

/** Kept in sync with Feedback.Categories on the backend. */
export const FEEDBACK_CATEGORIES = [
  'blocks',
  'tests',
  'review',
  'chat',
  'stats',
  'account',
  'other',
] as const

export type FeedbackCategory = (typeof FEEDBACK_CATEGORIES)[number]

export interface SubmitFeedbackInput {
  type: FeedbackType
  category?: string
  subject: string
  message: string
  /** 1–5; only sent for `review`. */
  rating?: number
  contactEmail?: string
  consent: boolean
  attachments: File[]
}

export interface SubmitFeedbackResult {
  id: string
  ticketNumber: number
  /** The code shown to the user, e.g. `LX-1042`. */
  ticketCode: string
}

export interface AdminFeedbackParams {
  page: number
  pageSize?: number
  type?: string
  status?: string
  category?: string
  search?: string
  dateFrom?: string
  dateTo?: string
}

export interface FeedbackAttachment {
  id: string
  fileName: string
  contentType: string
  sizeBytes: number
}

/** Admin view of one submission — includes the internal note the submitter never sees. */
export interface FeedbackDetail {
  id: string
  ticketNumber: number
  ticketCode: string
  userId: string | null
  userEmail: string | null
  type: FeedbackType
  category: string | null
  subject: string
  message: string
  rating: number | null
  contactEmail: string | null
  status: FeedbackStatus
  adminNote: string | null
  resolvedBy: string | null
  resolvedAt: string | null
  createdAt: string
  updatedAt: string
  attachments: FeedbackAttachment[]
}

export interface FeedbackListItem {
  id: string
  ticketNumber: number
  ticketCode: string
  userId: string | null
  userEmail: string | null
  type: FeedbackType
  category: string | null
  subject: string
  rating: number | null
  status: FeedbackStatus
  createdAt: string
  attachmentCount: number
}
