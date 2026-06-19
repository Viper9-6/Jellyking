import type { Service, ServiceStatus } from '../types'

export type CreateServicePayload = Omit<Service, 'id'> & { secret?: string; username?: string; password?: string }
export type UpdateServicePayload = Partial<Omit<Service, 'id'>> & { secret?: string | null; username?: string | null; password?: string | null }

export async function fetchServices(): Promise<ServiceStatus[]> {
  const res = await fetch('/api/v1/services')
  if (!res.ok) throw new Error(`Failed to fetch services: ${res.status}`)
  return res.json()
}

export async function fetchServiceById(id: string): Promise<Service> {
  const res = await fetch(`/api/v1/services/${id}`)
  if (!res.ok) throw new Error(`Failed to fetch service: ${res.status}`)
  return res.json()
}

export async function createService(service: CreateServicePayload): Promise<Service> {
  const res = await fetch('/api/v1/services', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(service),
  })
  if (!res.ok) {
    const error = await res.json().catch(() => ({ message: 'Failed to create service' }))
    throw new Error(error.message || 'Failed to create service')
  }
  return res.json()
}

export async function updateService(id: string, service: UpdateServicePayload): Promise<Service> {
  const res = await fetch(`/api/v1/services/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(service),
  })
  if (!res.ok) {
    const error = await res.json().catch(() => ({ message: 'Failed to update service' }))
    throw new Error(error.message || 'Failed to update service')
  }
  return res.json()
}

export async function deleteService(id: string): Promise<void> {
  const res = await fetch(`/api/v1/services/${id}`, { method: 'DELETE' })
  if (!res.ok) throw new Error(`Failed to delete service: ${res.status}`)
}

export async function fetchTemplates(): Promise<Service[]> {
  const res = await fetch('/api/v1/templates')
  if (!res.ok) throw new Error(`Failed to fetch templates: ${res.status}`)
  return res.json()
}

export async function fetchServiceConfig(id: string): Promise<Service> {
  const res = await fetch(`/api/v1/services/${id}/config`)
  if (!res.ok) throw new Error(`Failed to load service config: ${res.status}`)
  return res.json()
}

export async function testService(req: import('../types').ServiceTestRequest): Promise<import('../types').ServiceTestResult> {
  const res = await fetch('/api/v1/services/test', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(req),
  })
  if (!res.ok) {
    const error = await res.json().catch(() => ({ message: 'Connection test failed' }))
    throw new Error(error.message || 'Connection test failed')
  }
  return res.json()
}
