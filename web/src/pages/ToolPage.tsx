import { useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { useParams } from 'react-router'
import { api } from '../api/client'
import { availabilityQuery } from '../api/queries'
import type { Tool, ToolDay } from '../api/types'
import { useAuth } from '../auth/useAuth'
import { AskBox } from '../components/AskBox'
import { AvailabilityBadge } from '../components/AvailabilityBadge'
import { BookingActions } from '../components/BookingActions'
import { BookingForm } from '../components/BookingForm'
import { DocumentsPanel } from '../components/DocumentsPanel'
import { ProblemMessage } from '../components/ProblemMessage'
import { formatTime, labToday } from '../time'

export function ToolPage() {
  const { toolId = '' } = useParams()
  const { isAdmin } = useAuth()
  const [date, setDate] = useState(labToday())

  const tool = useQuery({ queryKey: ['tool', toolId], queryFn: () => api<Tool>(`/tools/${toolId}`) })
  const availability = useQuery(availabilityQuery(toolId))
  const day = useQuery({
    queryKey: ['bookings', 'tool', toolId, date],
    queryFn: () => api<ToolDay>(`/tools/${toolId}/bookings?date=${date}`),
  })

  if (tool.error) return <ProblemMessage error={tool.error} />
  if (!tool.data) return <p className="muted">Loading…</p>

  return (
    <main>
      <div className="title-row">
        <h1>{tool.data.name}</h1>
        <AvailabilityBadge toolId={toolId} />
      </div>
      <p className="muted">
        {tool.data.type} · {tool.data.location}
        {!tool.data.isActive && <strong> · Out of service</strong>}
      </p>
      <p>{tool.data.description}</p>
      {availability.data && <p className="explanation">{availability.data.explanation}</p>}

      <section className="card">
        <div className="title-row">
          <h2>Schedule</h2>
          <input type="date" value={date} onChange={(e) => setDate(e.target.value || labToday())} aria-label="Day" />
        </div>
        <ProblemMessage error={day.error} />
        {day.data && day.data.bookings.length === 0 && <p className="muted">No bookings on this day.</p>}
        <ul className="day-view">
          {day.data?.bookings.map((b) => (
            <li key={b.id} className={`slot slot-${b.status.toLowerCase()}`}>
              <span className="slot-time">
                {formatTime(b.start)}–{formatTime(b.end)}
              </span>
              <span>
                <strong>{b.userDisplayName ?? 'Unknown user'}</strong> · {b.purpose}
                <span className="muted"> · {b.status}</span>
              </span>
              <BookingActions booking={b} />
            </li>
          ))}
        </ul>
      </section>

      {tool.data.isActive && <BookingForm tool={tool.data} date={date} />}
      <AskBox toolId={toolId} toolName={tool.data.name} />
      {isAdmin && <DocumentsPanel toolId={toolId} />}
    </main>
  )
}
