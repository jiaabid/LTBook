import { useState, type FormEvent } from 'react'
import { useAuth } from '../auth/useAuth'
import { ProblemMessage } from '../components/ProblemMessage'

export function LoginPage() {
  const { login, register } = useAuth()
  const [mode, setMode] = useState<'login' | 'register'>('login')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [displayName, setDisplayName] = useState('')
  const [error, setError] = useState<unknown>(null)
  const [busy, setBusy] = useState(false)

  const submit = async (e: FormEvent) => {
    e.preventDefault()
    setBusy(true)
    setError(null)
    try {
      if (mode === 'login') await login(email, password)
      else await register(email, password, displayName)
    } catch (err) {
      setError(err)
    } finally {
      setBusy(false)
    }
  }

  return (
    <main className="narrow">
      <h1>ToolBook</h1>
      <p className="muted">Book shared lab instruments, see when they will be free, and ask how to use them.</p>
      <form className="card" onSubmit={submit}>
        <h2>{mode === 'login' ? 'Sign in' : 'Create an account'}</h2>
        {mode === 'register' && (
          <label>
            Display name
            <input value={displayName} onChange={(e) => setDisplayName(e.target.value)} autoComplete="name" />
          </label>
        )}
        <label>
          Email
          <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} autoComplete="email" required />
        </label>
        <label>
          Password
          <input
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            autoComplete={mode === 'login' ? 'current-password' : 'new-password'}
            required
          />
        </label>
        {mode === 'register' && (
          <p className="muted">At least 6 characters with an uppercase letter, a digit and a symbol.</p>
        )}
        <ProblemMessage error={error} />
        <button type="submit" disabled={busy}>
          {mode === 'login' ? 'Sign in' : 'Register'}
        </button>
        <button type="button" className="link" onClick={() => setMode(mode === 'login' ? 'register' : 'login')}>
          {mode === 'login' ? 'No account? Register' : 'Have an account? Sign in'}
        </button>
      </form>
    </main>
  )
}
