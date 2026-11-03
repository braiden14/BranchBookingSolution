import { useEffect, useRef, type ReactNode } from 'react'

interface ConfirmationDialogProps {
  title: string
  description: ReactNode
  confirmLabel: string
  isConfirming?: boolean
  onConfirm: () => void
  onClose: () => void
}

export function ConfirmationDialog({ title, description, confirmLabel, isConfirming = false, onConfirm, onClose }: ConfirmationDialogProps) {
  const confirmButtonRef = useRef<HTMLButtonElement>(null)
  const previousFocusRef = useRef<HTMLElement | null>(null)

  useEffect(() => {
    previousFocusRef.current = document.activeElement as HTMLElement | null
    confirmButtonRef.current?.focus()
    const closeOnEscape = (event: KeyboardEvent) => { if (event.key === 'Escape' && !isConfirming) onClose() }
    window.addEventListener('keydown', closeOnEscape)
    return () => {
      window.removeEventListener('keydown', closeOnEscape)
      previousFocusRef.current?.focus()
    }
  }, [onClose])

  return <div className="dialog-backdrop" role="presentation" onMouseDown={event => { if (event.target === event.currentTarget && !isConfirming) onClose() }}>
    <section className="dialog confirmation-dialog" role="alertdialog" aria-modal="true" aria-labelledby="confirmation-dialog-title" aria-describedby="confirmation-dialog-description">
      <h2 id="confirmation-dialog-title">{title}</h2>
      <p id="confirmation-dialog-description">{description}</p>
      <div className="configuration-actions">
        <button type="button" className="button button--secondary" onClick={onClose} disabled={isConfirming}>Cancel</button>
        <button ref={confirmButtonRef} type="button" className="button button--danger" onClick={onConfirm} disabled={isConfirming} aria-busy={isConfirming}>{isConfirming ? 'Working…' : confirmLabel}</button>
      </div>
    </section>
  </div>
}
