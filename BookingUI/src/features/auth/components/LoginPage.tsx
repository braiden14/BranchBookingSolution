import { ArrowRight, LockKeyhole } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../providers/AuthProvider'

export function LoginPage() {
  const { signIn } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [showPassword, setShowPassword] = useState(false)
  const message = (location.state as { message?: string } | null)?.message

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setIsSubmitting(true)
    setError(null)
    try {
      const signedInUser = await signIn(email, password)
      const requestedDestination = (location.state as { from?: { pathname?: string } } | null)?.from?.pathname
      const destination = signedInUser.role === 'customer'
        ? '/appointments'
        : requestedDestination ?? '/'
      navigate(destination, { replace: true })
    } catch (loginError) {
      setError(loginError instanceof Error ? loginError.message : 'Sign in failed. Please try again.')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <main className="auth-page">
      <section className="auth-card">
        <div className="brand-mark brand-mark--large">B</div>
        <p className="eyebrow">BranchBook</p>
        <h1>Welcome back</h1>
        <p className="muted">Sign in to manage your branches and appointments.</p>
        <form className="login-form" onSubmit={submit}>
          <label htmlFor="login-email">Email address<input id="login-email" required type="email" autoComplete="email" value={email} onChange={(event) => setEmail(event.target.value)} placeholder="you@company.co.za" aria-invalid={Boolean(error)} aria-describedby={error ? 'login-error' : undefined} /></label>
          <label htmlFor="login-password">Password<div className="password-input"><input id="login-password" required type={showPassword ? 'text' : 'password'} autoComplete="current-password" value={password} onChange={(event) => setPassword(event.target.value)} placeholder="••••••••" aria-invalid={Boolean(error)} aria-describedby={error ? 'login-error' : undefined} /><button type="button" className="password-toggle" onClick={() => setShowPassword(value => !value)}>{showPassword ? 'Hide' : 'Show'}</button></div></label>
          {error && <p id="login-error" className="form-error" role="alert">{error}</p>}
          {message && <p className="form-success">{message}</p>}
          <button className="button button--primary button--full" type="submit" disabled={isSubmitting} aria-busy={isSubmitting}>{isSubmitting ? 'Signing in…' : <>Sign in <ArrowRight size={17} /></>}</button>
        </form>
        <p className="secure-note"><LockKeyhole size={15} /> Secure access for your team</p>
        <p className="auth-switch">New to BranchBook? <Link to="/register">Create a customer account</Link></p>
      </section>
    </main>
  )
}
