export const authService = {
  login: async (_email: string, _password: string) => {
    throw new Error('Authentication API is not connected yet.')
  },
  logout: async () => undefined,
}
