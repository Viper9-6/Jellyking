import type { UserDto, CreateUserRequest, UpdateUserRequest } from '../types'

export async function fetchUsers(): Promise<UserDto[]> {
  const res = await fetch('/api/v1/users')
  if (!res.ok) throw new Error(`Failed to load users: ${res.status}`)
  return res.json()
}

export async function fetchUserById(id: string): Promise<UserDto> {
  const res = await fetch(`/api/v1/users/${id}`)
  if (!res.ok) throw new Error(`Failed to load user: ${res.status}`)
  return res.json()
}

export async function createUser(request: CreateUserRequest): Promise<UserDto> {
  const res = await fetch('/api/v1/users', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  })
  if (!res.ok) {
    const error = await res.json().catch(() => ({ message: 'Failed to create user' }))
    throw new Error(error.message || 'Failed to create user')
  }
  return res.json()
}

export async function updateUser(id: string, request: UpdateUserRequest): Promise<UserDto> {
  const res = await fetch(`/api/v1/users/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  })
  if (!res.ok) {
    const error = await res.json().catch(() => ({ message: 'Failed to update user' }))
    throw new Error(error.message || 'Failed to update user')
  }
  return res.json()
}

export async function deleteUser(id: string): Promise<void> {
  const res = await fetch(`/api/v1/users/${id}`, { method: 'DELETE' })
  if (!res.ok) throw new Error(`Failed to delete user: ${res.status}`)
}
