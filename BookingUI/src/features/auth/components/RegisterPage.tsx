import { ArrowRight, LockKeyhole } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { authService } from '../services/auth.service'

export function RegisterPage() {
  const navigate = useNavigate()
  const [form, setForm] = useState({ email: '', password: '', firstName: '', lastName: '', phoneNumber: '' })
  const [confirmPassword, setConfirmPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  const update = (field: keyof typeof form) => (value: string) => setForm((current) => ({ ...current, [field]: value }))

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setIsSubmitting(true)
    setError(null)
    if (form.password !== confirmPassword) { setError('Passwords do not match.'); setIsSubmitting(false); return }
    try {
      await authService.register({ ...form, phoneNumber: form.phoneNumber || null })
      navigate('/login', { replace: true, state: { message: 'Your account was created. Sign in to continue.' } })
    } catch (registrationError) {
      setError(registrationError instanceof Error ? registrationError.message : 'We could not create your account. Please try again.')
    } finally {
      setIsSubmitting(false)
    }
  }

  return <main className="auth-page">
    <section className="auth-card">
      <div className="brand-mark brand-mark--large">B</div>
      <p className="eyebrow">BranchBook</p>
      <h1>Create your account</h1>
      <p className="muted">Create a customer account to book appointments with our branches.</p>
      <form className="login-form" onSubmit={submit}>
        <div className="form-grid">
          <label htmlFor="register-first-name">First name<input id="register-first-name" required autoComplete="given-name" value={form.firstName} onChange={(event) => update('firstName')(event.target.value)} /></label>
          <label htmlFor="register-last-name">Last name<input id="register-last-name" required autoComplete="family-name" value={form.lastName} onChange={(event) => update('lastName')(event.target.value)} /></label>
        </div>
        <label htmlFor="register-email">Email address<input id="register-email" required type="email" autoComplete="email" value={form.email} onChange={(event) => update('email')(event.target.value)} placeholder="you@example.com" /></label>
        <label htmlFor="register-phone">Phone number <span className="optional">Optional</span><input id="register-phone" type="tel" autoComplete="tel" value={form.phoneNumber} onChange={(event) => update('phoneNumber')(event.target.value)} placeholder="+27 12 345 6789" /></label>
        <label htmlFor="register-password">Password<input id="register-password" required minLength={8} type="password" autoComplete="new-password" value={form.password} onChange={(event) => update('password')(event.target.value)} placeholder="At least 8 characters" aria-describedby="password-hint" /></label>
        <span id="password-hint" className="form-hint">Use at least 8 characters.</span>
        <label htmlFor="register-confirm-password">Confirm password<input id="register-confirm-password" required minLength={8} type="password" autoComplete="new-password" value={confirmPassword} onChange={(event) => setConfirmPassword(event.target.value)} /></label>
        {error && <p className="form-error" role="alert">{error}</p>}
        <button className="button button--primary button--full" type="submit" disabled={isSubmitting} aria-busy={isSubmitting}>{isSubmitting ? 'Creating account…' : <>Create account <ArrowRight size={17} /></>}</button>
      </form>
      <p className="secure-note"><LockKeyhole size={15} /> Your account details are protected.</p>
      <p className="auth-switch">Already have an account? <Link to="/login">Sign in</Link></p>
    </section>
  </main>
}
