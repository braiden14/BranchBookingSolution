import { ArrowRight, LockKeyhole } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { authService } from '../services/auth.service'

export function AcceptInvitationPage() {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const token = searchParams.get('token')?.trim() ?? ''
  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setError(null)
    if (!token) { setError('This invitation link is missing its token. Ask an administrator to issue a new invitation.'); return }
    if (password.length < 8) { setError('Password must be at least 8 characters long.'); return }
    if (password !== confirmPassword) { setError('Passwords do not match.'); return }
    setIsSubmitting(true)
    try {
      await authService.acceptInvitation(token, password)
      navigate('/login', { replace: true, state: { message: 'Your staff account is ready. Sign in to continue.' } })
    } catch (acceptanceError) {
      setError(acceptanceError instanceof Error ? acceptanceError.message : 'This invitation could not be accepted. It may be expired or invalid.')
    } finally { setIsSubmitting(false) }
  }

  return <main className="auth-page">
    <section className="auth-card">
      <div className="brand-mark brand-mark--large">B</div>
      <p className="eyebrow">BranchBook</p>
      <h1>Set your password</h1>
      <p className="muted">Complete your staff invitation to access the BranchBook workspace.</p>
      <form className="login-form" onSubmit={submit}>
        <label htmlFor="invitation-password">Password<input id="invitation-password" required minLength={8} type="password" autoComplete="new-password" value={password} onChange={event => setPassword(event.target.value)} placeholder="At least 8 characters" aria-describedby="invitation-password-hint" /></label>
        <span id="invitation-password-hint" className="form-hint">Use at least 8 characters.</span>
        <label htmlFor="invitation-confirm-password">Confirm password<input id="invitation-confirm-password" required minLength={8} type="password" autoComplete="new-password" value={confirmPassword} onChange={event => setConfirmPassword(event.target.value)} /></label>
        {error && <p className="form-error" role="alert">{error}</p>}
        <button className="button button--primary button--full" type="submit" disabled={isSubmitting || !token} aria-busy={isSubmitting}>{isSubmitting ? 'Activating account…' : <>Accept invitation <ArrowRight size={17} /></>}</button>
      </form>
      <p className="secure-note"><LockKeyhole size={15} /> Your account details are protected.</p>
      <p className="auth-switch">Already accepted? <Link to="/login">Sign in</Link></p>
    </section>
  </main>
}
