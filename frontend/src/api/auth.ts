import type { MeDto, SetupRequest, LoginRequest } from '../types'

export async function checkSetupRequired(): Promise<boolean> {
  const res = await fetch('/api/v1/auth/setup-required')
  if (!res.ok) throw new Error(`Failed to check setup: ${res.status}`)
  return res.json()
}

export async function setupAdmin(request: SetupRequest): Promise<void> {
  const res = await fetch('/api/v1/auth/setup', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  })
  if (!res.ok) {
    const error = await res.json().catch(() => ({ message: 'Setup failed' }))
    throw new Error(error.message || 'Setup failed')
  }
}

export async function login(request: LoginRequest): Promise<void> {
  const res = await fetch('/api/v1/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  })
  if (!res.ok) {
    if (res.status === 401) {
      throw new Error('Invalid username or password')
    }
    const error = await res.json().catch(() => ({ message: 'Login failed' }))
    throw new Error(error.message || 'Login failed')
  }
}

export async function logout(): Promise<void> {
  const res = await fetch('/api/v1/auth/logout', { method: 'POST' })
  if (!res.ok) throw new Error('Logout failed')
}

export async function getMe(): Promise<MeDto | null> {
  const res = await fetch('/api/v1/auth/me')
  if (res.status === 401) return null
  if (!res.ok) throw new Error(`Failed to get user: ${res.status}`)
  return res.json()
}
