import { useCallback, useEffect, useRef, useState } from 'react'
import type { ServiceStatus } from '../types'

const POLL_INTERVAL_MS = 15_000

async function fetchServices(): Promise<ServiceStatus[]> {
  const res = await fetch('/api/v1/services')
  if (!res.ok) throw new Error(`Failed to fetch services: ${res.status}`)
  return res.json()
}

export function useServices() {
  const [services, setServices]   = useState<ServiceStatus[]>([])
  const [loading, setLoading]     = useState(true)
  const [error, setError]         = useState<string | null>(null)
  const intervalRef               = useRef<ReturnType<typeof setInterval> | null>(null)

  const refresh = useCallback(async () => {
    try {
      const data = await fetchServices()
      setServices(data)
      setError(null)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error')
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    refresh()
    intervalRef.current = setInterval(refresh, POLL_INTERVAL_MS)
    return () => {
      if (intervalRef.current) clearInterval(intervalRef.current)
    }
  }, [refresh])

  return { services, loading, error, refresh }
}
