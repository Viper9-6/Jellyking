import type { SettingsDto, UpdateSettingsRequest } from '../types'

export async function getSettings(): Promise<SettingsDto> {
  const res = await fetch('/api/v1/settings')
  if (!res.ok) throw new Error(`Failed to load settings: ${res.status}`)
  return res.json()
}

export async function updateSettings(request: UpdateSettingsRequest): Promise<SettingsDto> {
  const res = await fetch('/api/v1/settings', {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  })
  if (!res.ok) {
    const error = await res.json().catch(() => ({ message: 'Failed to save settings' }))
    throw new Error(error.message || 'Failed to save settings')
  }
  return res.json()
}
