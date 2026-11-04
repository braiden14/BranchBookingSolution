import { useEffect, useRef, type ReactNode } from 'react'

interface ConfirmationDialogProps {
  title: string
  description: ReactNode
  confirmLabel: string
  isConfirming?: boolean
  showCancel?: boolean
  tone?: 'danger' | 'primary'
  onConfirm: () => void
  onClose: () => void
}

export function ConfirmationDialog({ title, description, confirmLabel, isConfirming = false, showCancel = true, tone = 'danger', onConfirm, onClose }: ConfirmationDialogProps) {
  const confirmButtonRef = useRef<HTMLButtonElement>(null)
  const dialogRef = useRef<HTMLElement>(null)
  const previousFocusRef = useRef<HTMLElement | null>(null)
  // Callers pass inline handlers; refs keep the effect from re-running (and stealing focus) on every render.
  const onCloseRef = useRef(onClose)
  const isConfirmingRef = useRef(isConfirming)
  onCloseRef.current = onClose
  isConfirmingRef.current = isConfirming

  useEffect(() => {
    previousFocusRef.current = document.activeElement as HTMLElement | null
    confirmButtonRef.current?.focus()
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape' && !isConfirmingRef.current) { onCloseRef.current(); return }
      if (event.key !== 'Tab' || !dialogRef.current) return
      const focusable = Array.from(dialogRef.current.querySelectorAll<HTMLElement>('button:not([disabled]), [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'))
      if (!focusable.length) { event.preventDefault(); return }
      const first = focusable[0]
      const last = focusable[focusable.length - 1]
      if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus() }
      else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus() }
      else if (!dialogRef.current.contains(document.activeElement)) { event.preventDefault(); first.focus() }
    }
    window.addEventListener('keydown', onKeyDown)
    return () => {
      window.removeEventListener('keydown', onKeyDown)
      previousFocusRef.current?.focus()
    }
  }, [])

  return <div className="dialog-backdrop" role="presentation" onMouseDown={event => { if (event.target === event.currentTarget && !isConfirming) onClose() }}>
    <section ref={dialogRef} className="dialog confirmation-dialog" role="alertdialog" aria-modal="true" aria-labelledby="confirmation-dialog-title" aria-describedby="confirmation-dialog-description">
      <h2 id="confirmation-dialog-title">{title}</h2>
      <p id="confirmation-dialog-description">{description}</p>
       <div className="configuration-actions">
         {showCancel && <button type="button" className="button button--secondary" onClick={onClose} disabled={isConfirming}>Cancel</button>}
        <button ref={confirmButtonRef} type="button" className={`button ${tone === 'danger' ? 'button--danger' : 'button--primary'}`} onClick={onConfirm} disabled={isConfirming} aria-busy={isConfirming}>{isConfirming ? 'Working…' : confirmLabel}</button>
      </div>
    </section>
  </div>
}
