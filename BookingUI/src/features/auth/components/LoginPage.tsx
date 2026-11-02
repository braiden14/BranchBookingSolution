import { ArrowRight, LockKeyhole } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../providers/AuthProvider'

export function LoginPage() {
  const { signIn } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setIsSubmitting(true)
    setError(null)
    try {
      await signIn(email, password)
      const destination = (location.state as { from?: { pathname?: string } } | null)?.from?.pathname ?? '/'
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
          <label>Email address<input required type="email" value={email} onChange={(event) => setEmail(event.target.value)} placeholder="you@company.co.za" /></label>
          <label>Password<input required type="password" value={password} onChange={(event) => setPassword(event.target.value)} placeholder="••••••••" /></label>
          {error && <p className="form-error">{error}</p>}
          <button className="button button--primary button--full" type="submit" disabled={isSubmitting}>{isSubmitting ? 'Signing in…' : <>Sign in <ArrowRight size={17} /></>}</button>
        </form>
        <p className="secure-note"><LockKeyhole size={15} /> Secure access for your team</p>
      </section>
    </main>
  )
}
