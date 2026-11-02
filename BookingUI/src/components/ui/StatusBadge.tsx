import type { ReactNode } from 'react'

export function StatusBadge({ children, tone = 'active' }: { children: ReactNode; tone?: 'active' | 'inactive' | 'neutral' }) {
  return <span className={`status-badge status-badge--${tone}`}><span className="status-badge__dot" />{children}</span>
}
