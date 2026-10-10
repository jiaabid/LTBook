import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router'
import { api } from '../api/client'
import type { Tool } from '../api/types'
import { AvailabilityBadge } from '../components/AvailabilityBadge'
import { ProblemMessage } from '../components/ProblemMessage'

const toolsQuery = { queryKey: ['tools'], queryFn: () => api<Tool[]>('/tools') }

export function ToolsPage() {
  const tools = useQuery(toolsQuery)

  return (
    <main>
      <h1>Tools</h1>
      <ProblemMessage error={tools.error} />
      {tools.isPending && <p className="muted">Loading…</p>}
      <ul className="tool-list">
        {tools.data?.map((tool) => (
          <li key={tool.id} className="card tool-card">
            <div>
              <Link to={`/tools/${tool.id}`} className="tool-name">
                {tool.name}
              </Link>
              <div className="muted">
                {tool.type} · {tool.location}
              </div>
            </div>
            <AvailabilityBadge toolId={tool.id} />
          </li>
        ))}
      </ul>
    </main>
  )
}
