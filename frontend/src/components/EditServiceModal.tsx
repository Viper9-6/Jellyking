import { useEffect, useState } from 'react'
import type { ServiceFormState } from '../types'
import { fetchServiceConfig, updateService } from '../api/services'
import { ServiceFormFields } from './ServiceFormFields'
import './AddServiceModal.css'

interface Props {
  serviceId: string          // the persistent Guid (ServiceStatus.serviceId)
  onClose: () => void
  onSaved: () => void
}

export function EditServiceModal({ serviceId, onClose, onSaved }: Props) {
  const [state, setState] = useState<ServiceFormState | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    fetchServiceConfig(serviceId)
      .then(s => setState({
        slug: s.slug, name: s.name, host: s.host, port: s.port, basePath: s.basePath,
        healthPath: s.healthPath, icon: s.icon, webSocketPaths: s.webSocketPaths,
        priority: s.priority, enabled: s.enabled, authType: s.authType ?? 'none',
        secret: '', username: '', password: '',
      }))
      .catch(e => setError(e instanceof Error ? e.message : 'Failed to load service'))
  }, [serviceId])

  if (error && !state) {
    return (
      <div className="modal-overlay" onClick={onClose}>
        <div className="modal" onClick={e => e.stopPropagation()}>
          <header className="modal-header"><h2>Edit Service</h2><button className="modal-close" onClick={onClose}>&times;</button></header>
          <div className="modal-body"><div className="form-error">{error}</div>
            <footer className="modal-footer"><button className="btn-primary" onClick={onClose}>Close</button></footer>
          </div>
        </div>
      </div>
    )
  }
  if (!state) {
    return (
      <div className="modal-overlay" onClick={onClose}>
        <div className="modal" onClick={e => e.stopPropagation()}>
          <header className="modal-header"><h2>Edit Service</h2><button className="modal-close" onClick={onClose}>&times;</button></header>
          <div className="modal-body"><p className="form-hint">Loading…</p></div>
        </div>
      </div>
    )
  }

  const update = (patch: Partial<ServiceFormState>) => setState(prev => prev ? { ...prev, ...patch } : prev)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault(); setError(null); setSubmitting(true)
    try {
      const payload: Parameters<typeof updateService>[1] = {
        slug: state.slug, name: state.name, host: state.host, port: state.port,
        basePath: state.basePath, healthPath: state.healthPath, icon: state.icon,
        webSocketPaths: state.webSocketPaths, priority: state.priority, enabled: state.enabled,
        authType: state.authType,
      }
      // Credential fields are write-only: in edit mode, blank = keep existing.
      if (state.authType === 'qbittorrent') {
        if (state.username.trim()) payload.username = state.username.trim()
        if (state.password.trim()) payload.password = state.password.trim()
      } else if (state.authType === 'none') {
        payload.secret = ''   // switching to none clears the stored credential
      } else {
        if (state.secret.trim()) payload.secret = state.secret.trim()
      }
      await updateService(serviceId, payload)
      onSaved()
      onClose()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save service')
    } finally { setSubmitting(false) }
  }

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal" onClick={e => e.stopPropagation()}>
        <header className="modal-header">
          <h2>Edit Service</h2>
          <button className="modal-close" onClick={onClose}>&times;</button>
        </header>
        <form onSubmit={handleSubmit} className="modal-body">
          <ServiceFormFields state={state} update={update} mode="edit" />
          {error && <div className="form-error">{error}</div>}
          <footer className="modal-footer">
            <button type="button" className="btn-secondary" onClick={onClose}>Cancel</button>
            <button type="submit" className="btn-primary" disabled={submitting}>
              {submitting ? 'Saving…' : 'Save changes'}
            </button>
          </footer>
        </form>
      </div>
    </div>
  )
}
