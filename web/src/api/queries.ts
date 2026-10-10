import { api } from './client'
import type { Availability } from './types'

export const availabilityQuery = (toolId: string) => ({
  queryKey: ['availability', toolId],
  queryFn: () => api<Availability>(`/tools/${toolId}/availability`),
  refetchInterval: 60_000,
})
