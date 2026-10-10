import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { Link } from 'react-router'
import { api, post } from '../api/client'
import type { Tool } from '../api/types'
import { ProblemMessage } from '../components/ProblemMessage'

const emptyTool = { name: '', type: '', location: '', description: '', defaultSlotMinutes: 60, typicalBookingMinutes: 60 }

/** Admin: add tools and take them out of service. Manuals are uploaded on each tool's page. */
export function AdminPage() {
  const queryClient = useQueryClient()
  const [form, setForm] = useState(emptyTool)
  const tools = useQuery({ queryKey: ['tools', 'all'], queryFn: () => api<Tool[]>('/tools?includeInactive=true') })

  const refresh = () => queryClient.invalidateQueries({ queryKey: ['tools'] })
  const create = useMutation({
    mutationFn: () => post<Tool>('/tools', form),
    onSuccess: () => {
      setForm(emptyTool)
      void refresh()
    },
  })
  const deactivate = useMutation({ mutationFn: (id: string) => post(`/tools/${id}/deactivate`), onSuccess: refresh })

  const field = (key: keyof typeof emptyTool, label: string, type = 'text') => (
    <label>
      {label}
      <input
        type={type}
        value={form[key]}
        onChange={(e) => setForm({ ...form, [key]: type === 'number' ? Number(e.target.value) : e.target.value })}
      />
    </label>
  )

  const submit = (e: FormEvent) => {
    e.preventDefault()
    create.mutate()
  }

  return (
    <main>
      <h1>Admin</h1>
      <form className="card" onSubmit={submit}>
        <h2>Add a tool</h2>
        <div className="grid-3">
          {field('name', 'Name')}
          {field('type', 'Type')}
          {field('location', 'Location')}
          {field('defaultSlotMinutes', 'Default slot (min)', 'number')}
          {field('typicalBookingMinutes', 'Typical booking (min)', 'number')}
        </div>
        {field('description', 'Description')}
        <ProblemMessage error={create.error} />
        <button type="submit" disabled={create.isPending}>
          Add tool
        </button>
      </form>

      <section className="card">
        <h2>All tools</h2>
        <ProblemMessage error={deactivate.error} />
        <table>
          <tbody>
            {tools.data?.map((t) => (
              <tr key={t.id}>
                <td>
                  <Link to={`/tools/${t.id}`}>{t.name}</Link>
                </td>
                <td>{t.isActive ? 'Active' : 'Out of service'}</td>
                <td>
                  {t.isActive && (
                    <button
                      className="secondary"
                      onClick={() => {
                        if (confirm(`Take ${t.name} out of service? Its future bookings will be cancelled.`)) deactivate.mutate(t.id)
                      }}
                    >
                      Deactivate
                    </button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </section>
    </main>
  )
}
