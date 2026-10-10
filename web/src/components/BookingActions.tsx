import { useMutation, useQueryClient } from '@tanstack/react-query'
import { post } from '../api/client'
import type { Booking } from '../api/types'
import { useAuth } from '../auth/useAuth'
import { ProblemMessage } from './ProblemMessage'

/** Cancel / check in / check out buttons, shown according to status and ownership. */
export function BookingActions({ booking }: { booking: Booking }) {
  const { user, isAdmin } = useAuth()
  const queryClient = useQueryClient()
  const action = useMutation({
    mutationFn: (verb: 'cancel' | 'check-in' | 'check-out') => post(`/bookings/${booking.id}/${verb}`),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['bookings'] })
      void queryClient.invalidateQueries({ queryKey: ['availability', booking.toolId] })
    },
  })

  const isOwner = user?.id === booking.userId
  const canManage = isOwner || isAdmin

  return (
    <div className="actions">
      {booking.status === 'Scheduled' && isOwner && (
        <button onClick={() => action.mutate('check-in')} disabled={action.isPending}>
          Check in
        </button>
      )}
      {booking.status === 'InUse' && canManage && (
        <button onClick={() => action.mutate('check-out')} disabled={action.isPending}>
          Check out
        </button>
      )}
      {booking.status === 'Scheduled' && canManage && (
        <button className="secondary" onClick={() => action.mutate('cancel')} disabled={action.isPending}>
          Cancel
        </button>
      )}
      <ProblemMessage error={action.error} />
    </div>
  )
}
