import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useRef } from 'react'
import { api } from '../api/client'
import type { ToolDocument } from '../api/types'
import { formatDateTime } from '../time'
import { ProblemMessage } from './ProblemMessage'

/** Admin: upload manuals and watch their ingestion status (polls while anything is still in progress). */
export function DocumentsPanel({ toolId }: { toolId: string }) {
  const queryClient = useQueryClient()
  const fileInput = useRef<HTMLInputElement>(null)

  const documents = useQuery({
    queryKey: ['documents', toolId],
    queryFn: () => api<ToolDocument[]>(`/tools/${toolId}/documents`),
    refetchInterval: (q) => (q.state.data?.some((d) => d.status === 'Pending' || d.status === 'Processing') ? 2000 : false),
  })

  const upload = useMutation({
    mutationFn: (file: File) => {
      const form = new FormData()
      form.append('file', file)
      return api<ToolDocument>(`/tools/${toolId}/documents`, { method: 'POST', body: form })
    },
    onSuccess: () => {
      if (fileInput.current) fileInput.current.value = ''
      void queryClient.invalidateQueries({ queryKey: ['documents', toolId] })
    },
  })

  return (
    <section className="card">
      <h2>Manuals (admin)</h2>
      <div className="row">
        <input ref={fileInput} type="file" accept=".pdf,.md,.txt" aria-label="Manual file" />
        <button
          onClick={() => fileInput.current?.files?.[0] && upload.mutate(fileInput.current.files[0])}
          disabled={upload.isPending}
        >
          {upload.isPending ? 'Uploading…' : 'Upload'}
        </button>
      </div>
      <ProblemMessage error={upload.error} />
      {documents.data && documents.data.length > 0 ? (
        <table>
          <thead>
            <tr>
              <th>File</th>
              <th>Status</th>
              <th>Chunks</th>
              <th>Uploaded</th>
            </tr>
          </thead>
          <tbody>
            {documents.data.map((d) => (
              <tr key={d.id}>
                <td>{d.fileName}</td>
                <td>
                  <span className={`badge badge-doc-${d.status.toLowerCase()}`} title={d.error ?? undefined}>
                    {d.status}
                  </span>
                </td>
                <td>{d.chunkCount}</td>
                <td>{formatDateTime(d.uploadedAt)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      ) : (
        <p className="muted">No manuals uploaded yet.</p>
      )}
    </section>
  )
}
