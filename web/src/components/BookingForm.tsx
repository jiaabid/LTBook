import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { post } from '../api/client'
import type { Booking, Tool } from '../api/types'
import { labLocalToUtcIso } from '../time'
import { ProblemMessage } from './ProblemMessage'

function addMinutes(time: string, minutes: number): string {
  const [h, m] = time.split(':').map(Number)
  const total = Math.min(h * 60 + m + minutes, 23 * 60 + 59)
  return `${String(Math.floor(total / 60)).padStart(2, '0')}:${String(total % 60).padStart(2, '0')}`
}

/** Books a slot on the selected (Berlin) day. Times are entered in lab time and sent as UTC. */
export function BookingForm({ tool, date }: { tool: Tool; date: string }) {
  const queryClient = useQueryClient()
  const [start, setStart] = useState('09:00')
  const [end, setEnd] = useState(addMinutes('09:00', tool.typicalBookingMinutes))
  const [purpose, setPurpose] = useState('')

  const book = useMutation({
    mutationFn: () =>
      post<Booking>('/bookings', {
        toolId: tool.id,
        start: labLocalToUtcIso(date, start),
        end: labLocalToUtcIso(date, end),
        purpose,
      }),
    onSuccess: () => {
      setPurpose('')
      void queryClient.invalidateQueries({ queryKey: ['bookings'] })
      void queryClient.invalidateQueries({ queryKey: ['availability', tool.id] })
    },
  })

  const submit = (e: FormEvent) => {
    e.preventDefault()
    book.mutate()
  }

  return (
    <form className="card" onSubmit={submit}>
      <h2>Book a slot</h2>
      <div className="grid-3">
        <label>
          From
          <input
            type="time"
            value={start}
            onChange={(e) => {
              setStart(e.target.value)
              setEnd(addMinutes(e.target.value, tool.typicalBookingMinutes))
            }}
            required
          />
        </label>
        <label>
          To
          <input type="time" value={end} onChange={(e) => setEnd(e.target.value)} required />
        </label>
        <label>
          Purpose
          <input value={purpose} onChange={(e) => setPurpose(e.target.value)} placeholder="e.g. imaging sample A" maxLength={200} required />
        </label>
      </div>
      <p className="muted">
        Times are Berlin time. Typical session: {tool.typicalBookingMinutes} min.
      </p>
      <button type="submit" disabled={book.isPending}>
        {book.isPending ? 'Booking…' : 'Book'}
      </button>
      {book.isSuccess && <p className="success">Booked.</p>}
      <ProblemMessage error={book.error} />
    </form>
  )
}
