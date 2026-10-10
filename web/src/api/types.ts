// Shapes of the ToolBook API. All instants are ISO 8601 strings in UTC.

export interface CurrentUser {
  id: string
  email: string
  displayName: string
  roles: string[]
}

export interface Tool {
  id: string
  name: string
  type: string
  location: string
  description: string
  isActive: boolean
  defaultSlotMinutes: number
  typicalBookingMinutes: number
}

export type BookingStatus = 'Scheduled' | 'InUse' | 'Completed' | 'Cancelled'

export interface Booking {
  id: string
  toolId: string
  toolName: string | null
  userId: string
  userDisplayName: string | null
  start: string
  end: string
  purpose: string
  status: BookingStatus
  checkedInAt: string | null
  checkedOutAt: string | null
  cancellationReason: string | null
}

export interface ToolDay {
  toolId: string
  date: string
  timeZone: string
  bookings: Booking[]
}

export interface Availability {
  toolId: string
  status: 'Free' | 'InUse' | 'Booked'
  freeNow: boolean
  estimatedFreeAt: string
  nextFreeSlot: { start: string; end: string } | null
  confidence: 'Low' | 'Medium' | 'High'
  typicalOverrunMinutes: number | null
  historySampleSize: number
  explanation: string
}

export type DocumentStatus = 'Pending' | 'Processing' | 'Ready' | 'Failed'

export interface ToolDocument {
  id: string
  toolId: string
  fileName: string
  status: DocumentStatus
  error: string | null
  chunkCount: number
  uploadedAt: string
  processedAt: string | null
}

export interface Citation {
  number: number
  documentId: string
  fileName: string
  section: string | null
  page: number | null
  score: number
  excerpt: string
}

export interface AskResponse {
  answer: string
  grounded: boolean
  citations: Citation[]
  usedChunks: number
}

/** RFC 9457 problem details, as returned by every API error. */
export interface ProblemDetails {
  status?: number
  title?: string
  detail?: string
  code?: string
  errors?: Record<string, string[]>
}
