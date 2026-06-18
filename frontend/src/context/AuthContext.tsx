import { createContext, useCallback, useContext, useEffect, useState } from 'react'
import type { MeDto } from '../types'
import * as authApi from '../api/auth'

interface AuthContextValue {
  user: MeDto | null
  loading: boolean
  isSetupRequired: boolean
  login: (username: string, password: string) => Promise<void>
  logout: () => Promise<void>
  setupAdmin: (username: string, password: string) => Promise<void>
  refreshUser: () => Promise<void>
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<MeDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [isSetupRequired, setIsSetupRequired] = useState(false)

  const refreshUser = useCallback(async () => {
    try {
      const me = await authApi.getMe()
      setUser(me)
    } catch {
      setUser(null)
    }
  }, [])

  const checkSetup = useCallback(async () => {
    try {
      const required = await authApi.checkSetupRequired()
      setIsSetupRequired(required)
    } catch {
      setIsSetupRequired(false)
    }
  }, [])

  useEffect(() => {
    Promise.all([refreshUser(), checkSetup()]).finally(() => setLoading(false))
  }, [refreshUser, checkSetup])

  const login = useCallback(async (username: string, password: string) => {
    await authApi.login({ username, password })
    await refreshUser()
  }, [refreshUser])

  const logout = useCallback(async () => {
    await authApi.logout()
    setUser(null)
  }, [])

  const setupAdmin = useCallback(async (username: string, password: string) => {
    await authApi.setupAdmin({ username, password })
    await refreshUser()
    setIsSetupRequired(false)
  }, [refreshUser])

  return (
    <AuthContext.Provider value={{ user, loading, isSetupRequired, login, logout, setupAdmin, refreshUser }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}
