import { useQuery } from '@tanstack/react-query'
import { availabilityQuery } from '../api/queries'
import type { Availability } from '../api/types'
import { formatTime, minutesUntil } from '../time'

function label(a: Availability): string {
  if (a.freeNow) return 'Free now'
  const minutes = minutesUntil(a.estimatedFreeAt)
  const approx = a.confidence === 'Low' ? '' : '~'
  if (minutes === 0) return 'Free any minute'
  if (minutes < 90) return `Free in ${approx}${minutes} min`
  return `Free ${approx}${formatTime(a.estimatedFreeAt)}`
}

/** Live status badge ("Free now" / "Free in ~25 min"); the full explanation is in the tooltip. */
export function AvailabilityBadge({ toolId }: { toolId: string }) {
  const { data, isError } = useQuery(availabilityQuery(toolId))
  if (isError) return <span className="badge">Unknown</span>
  if (!data) return <span className="badge">…</span>

  const tone = data.freeNow ? 'free' : data.status === 'InUse' ? 'busy' : 'booked'
  return (
    <span className={`badge badge-${tone}`} title={data.explanation}>
      {label(data)}
    </span>
  )
}
