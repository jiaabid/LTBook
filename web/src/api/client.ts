import type { ProblemDetails } from './types'

const TOKEN_KEY = 'toolbook.accessToken'

// Note: localStorage is readable by any script on the page (XSS). Acceptable for this demo; see ADR-0005.
export const tokenStore = {
  get: () => localStorage.getItem(TOKEN_KEY),
  set: (token: string) => localStorage.setItem(TOKEN_KEY, token),
  clear: () => localStorage.removeItem(TOKEN_KEY),
}

/** An API error carrying the server's ProblemDetails. */
export class ApiError extends Error {
  readonly status: number
  readonly problem: ProblemDetails

  constructor(status: number, problem: ProblemDetails) {
    super(problem.detail ?? problem.title ?? `Request failed (${status})`)
    this.status = status
    this.problem = problem
  }
}

let onUnauthorized: () => void = () => {}
export const setUnauthorizedHandler = (handler: () => void) => {
  onUnauthorized = handler
}

export async function api<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers)
  const token = tokenStore.get()
  if (token) headers.set('Authorization', `Bearer ${token}`)
  if (init.body && !(init.body instanceof FormData)) headers.set('Content-Type', 'application/json')

  const response = await fetch(`/api${path}`, { ...init, headers })

  if (response.status === 401 && token) {
    tokenStore.clear()
    onUnauthorized()
  }

  if (!response.ok) {
    const problem: ProblemDetails = response.headers.get('content-type')?.includes('json')
      ? await response.json()
      : { title: response.statusText }
    if (response.status === 429) problem.detail ??= 'Too many requests. Please wait a minute and try again.'
    throw new ApiError(response.status, problem)
  }

  return response.status === 204 || response.headers.get('content-length') === '0'
    ? (undefined as T)
    : ((await response.json()) as T)
}

export const post = <T>(path: string, body?: unknown) =>
  api<T>(path, { method: 'POST', body: body === undefined ? undefined : JSON.stringify(body) })
