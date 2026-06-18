export async function changePassword(currentPassword: string, newPassword: string): Promise<void> {
  const res = await fetch('/api/v1/auth/change-password', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ currentPassword, newPassword }),
  })
  if (!res.ok) {
    if (res.status === 401) throw new Error('Current password is incorrect')
    const error = await res.json().catch(() => ({ message: 'Failed to change password' }))
    throw new Error(error.message || 'Failed to change password')
  }
}
