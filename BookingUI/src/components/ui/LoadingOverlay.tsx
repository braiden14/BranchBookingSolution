export function LoadingOverlay({ label }: { label: string }) {
  return <div className="save-loader" role="status" aria-live="polite"><span className="save-loader__spinner" />{label}</div>
}
