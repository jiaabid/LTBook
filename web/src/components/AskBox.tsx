import { useMutation, useQuery } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { api, post } from '../api/client'
import type { AskResponse } from '../api/types'
import { ProblemMessage } from './ProblemMessage'

/** "Ask about this tool": answers come only from this tool's manuals, with numbered citations. */
export function AskBox({ toolId, toolName }: { toolId: string; toolName: string }) {
  const [question, setQuestion] = useState('')
  const status = useQuery({ queryKey: ['assistant-status'], queryFn: () => api<{ enabled: boolean }>('/assistant/status') })
  const ask = useMutation({ mutationFn: (q: string) => post<AskResponse>(`/tools/${toolId}/ask`, { question: q }) })

  if (status.data && !status.data.enabled) {
    return (
      <section className="card">
        <h2>Ask about this tool</h2>
        <p className="muted">The assistant is disabled on this server (no AI provider configured).</p>
      </section>
    )
  }

  const submit = (e: FormEvent) => {
    e.preventDefault()
    if (question.trim()) ask.mutate(question.trim())
  }

  return (
    <section className="card">
      <h2>Ask about this tool</h2>
      <p className="muted">Answers are based only on the uploaded manuals for {toolName}.</p>
      <form onSubmit={submit} className="row">
        <input
          value={question}
          onChange={(e) => setQuestion(e.target.value)}
          placeholder="e.g. How do I vent the chamber?"
          maxLength={500}
          aria-label="Question"
        />
        <button type="submit" disabled={ask.isPending || !question.trim()}>
          {ask.isPending ? 'Asking…' : 'Ask'}
        </button>
      </form>

      <ProblemMessage error={ask.error} />

      {ask.data && (
        <div className={`answer ${ask.data.grounded ? '' : 'answer-notfound'}`}>
          <p className="answer-text">{ask.data.answer}</p>
          {ask.data.citations.length > 0 && (
            <ol className="citations">
              {ask.data.citations.map((c) => (
                <li key={c.number} value={c.number}>
                  <strong>{c.fileName}</strong>
                  {c.section && <> · {c.section}</>}
                  {c.page != null && <> · p. {c.page}</>}
                  <span className="muted"> (similarity {c.score.toFixed(2)})</span>
                  <blockquote>{c.excerpt}</blockquote>
                </li>
              ))}
            </ol>
          )}
        </div>
      )}
    </section>
  )
}
