export const queryClient = {
  invalidate: (key: string) => window.dispatchEvent(new CustomEvent(`query:${key}:invalidate`)),
}
