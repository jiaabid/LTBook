import { ApiError } from '../api/client'

/** Shows an API error: the ProblemDetails detail/title and, for validation problems, each field's messages. */
export function ProblemMessage({ error }: { error: unknown }) {
  if (!error) return null
  if (!(error instanceof ApiError)) {
    return <div className="alert">{error instanceof Error ? error.message : 'Something went wrong.'}</div>
  }

  const { title, detail, errors } = error.problem
  const fieldErrors = Object.entries(errors ?? {})
  return (
    <div className="alert" role="alert">
      <strong>{fieldErrors.length > 0 ? 'Please fix the following:' : (detail ?? title ?? 'Request failed')}</strong>
      {fieldErrors.length > 0 && (
        <ul>
          {fieldErrors.flatMap(([field, messages]) =>
            messages.map((m) => (
              <li key={field + m}>
                {/^[A-Z]/.test(field) && !m.includes(field) ? `${field}: ` : ''}
                {m}
              </li>
            )),
          )}
        </ul>
      )}
    </div>
  )
}
