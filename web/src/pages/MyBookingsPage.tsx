import { useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { Link } from 'react-router'
import { api } from '../api/client'
import type { Booking } from '../api/types'
import { BookingActions } from '../components/BookingActions'
import { ProblemMessage } from '../components/ProblemMessage'
import { formatDateTime, formatTime } from '../time'

export function MyBookingsPage() {
  const [includePast, setIncludePast] = useState(false)
  const bookings = useQuery({
    queryKey: ['bookings', 'mine', includePast],
    queryFn: () => api<Booking[]>(`/bookings/mine?includePast=${includePast}`),
  })

  return (
    <main>
      <div className="title-row">
        <h1>My bookings</h1>
        <label className="inline">
          <input type="checkbox" checked={includePast} onChange={(e) => setIncludePast(e.target.checked)} /> Show last 30 days
        </label>
      </div>
      <ProblemMessage error={bookings.error} />
      {bookings.data?.length === 0 && <p className="muted">No bookings. Pick a tool to book one.</p>}
      <ul className="day-view">
        {bookings.data?.map((b) => (
          <li key={b.id} className={`slot slot-${b.status.toLowerCase()}`}>
            <span className="slot-time">
              {formatDateTime(b.start)}–{formatTime(b.end)}
            </span>
            <span>
              <Link to={`/tools/${b.toolId}`}>
                <strong>{b.toolName ?? 'Tool'}</strong>
              </Link>{' '}
              · {b.purpose}
              <span className="muted"> · {b.status}</span>
              {b.cancellationReason && <span className="muted"> ({b.cancellationReason})</span>}
            </span>
            <BookingActions booking={b} />
          </li>
        ))}
      </ul>
    </main>
  )
}
