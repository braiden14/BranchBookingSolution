import { ArrowRight, LockKeyhole } from 'lucide-react'

export function LoginPage() {
  return (
    <main className="auth-page">
      <section className="auth-card">
        <div className="brand-mark brand-mark--large">B</div>
        <p className="eyebrow">BranchBook</p>
        <h1>Welcome back</h1>
        <p className="muted">Sign in to manage your branches and appointments.</p>
        <form className="login-form" onSubmit={(event) => event.preventDefault()}>
          <label>Email address<input type="email" placeholder="you@company.co.za" /></label>
          <label>Password<input type="password" placeholder="••••••••" /></label>
          <button className="button button--primary button--full" type="submit">Sign in <ArrowRight size={17} /></button>
        </form>
        <p className="secure-note"><LockKeyhole size={15} /> Secure access for your team</p>
      </section>
    </main>
  )
}
