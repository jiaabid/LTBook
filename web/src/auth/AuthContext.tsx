import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { api, post, setUnauthorizedHandler, tokenStore } from '../api/client'
import type { CurrentUser } from '../api/types'
import { AuthContext, type AuthState } from './useAuth'


export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient()
  const [token, setToken] = useState(tokenStore.get())

  const logout = useCallback(() => {
    tokenStore.clear()
    setToken(null)
    queryClient.clear()
  }, [queryClient])

  useEffect(() => setUnauthorizedHandler(logout), [logout])

  const me = useQuery({
    queryKey: ['me', token],
    queryFn: () => api<CurrentUser>('/users/me'),
    enabled: token !== null,
    staleTime: Infinity,
  })

  const login = useCallback(async (email: string, password: string) => {
    const result = await post<{ accessToken: string }>('/auth/login', { email, password })
    tokenStore.set(result.accessToken)
    setToken(result.accessToken)
  }, [])

  const register = useCallback(
    async (email: string, password: string, displayName: string) => {
      await post('/users/register', { email, password, displayName })
      await login(email, password)
    },
    [login],
  )

  const value = useMemo<AuthState>(
    () => ({
      user: me.data,
      isAdmin: me.data?.roles.includes('Admin') ?? false,
      isLoading: token !== null && me.isPending,
      login,
      register,
      logout,
    }),
    [me.data, me.isPending, token, login, register, logout],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
