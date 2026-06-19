import { useState, useEffect } from 'react'
import type { Service, ServiceFormState } from '../types'
import type { CreateServicePayload } from '../api/services'
import { fetchTemplates } from '../api/services'
import { ServiceFormFields } from './ServiceFormFields'
import './AddServiceModal.css'

interface Props {
  onClose: () => void
  onAdd: (service: CreateServicePayload) => Promise<void>
}

const emptyState: ServiceFormState = {
  slug: '', name: '', host: 'localhost', port: 8080, basePath: '', healthPath: '',
  icon: '', webSocketPaths: '', priority: 100, enabled: true, authType: 'none',
  secret: '', username: '', password: '',
}

export function AddServiceModal({ onClose, onAdd }: Props) {
  const [templates, setTemplates] = useState<Service[]>([])
  const [selectedTemplate, setSelectedTemplate] = useState<string>('')
  const [state, setState] = useState<ServiceFormState>(emptyState)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => { fetchTemplates().then(setTemplates).catch(console.error) }, [])

  useEffect(() => {
    if (!selectedTemplate) return
    const t = templates.find(x => x.slug === selectedTemplate)
    if (!t) return
    setState({
      slug: t.slug, name: t.name, host: t.host, port: t.port, basePath: t.basePath,
      healthPath: t.healthPath, icon: t.icon, webSocketPaths: t.webSocketPaths,
      priority: t.priority, enabled: t.enabled, authType: t.authType ?? 'none',
      secret: '', username: '', password: '',
    })
  }, [selectedTemplate, templates])

  const update = (patch: Partial<ServiceFormState>) => setState(prev => ({ ...prev, ...patch }))

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault(); setError(null); setSubmitting(true)
    try {
      const payload: CreateServicePayload = {
        slug: state.slug, name: state.name, host: state.host, port: state.port,
        basePath: state.basePath, healthPath: state.healthPath, icon: state.icon,
        webSocketPaths: state.webSocketPaths, priority: state.priority, enabled: state.enabled,
        authType: state.authType,
      }
      if (state.authType === 'qbittorrent') {
        payload.username = state.username.trim() || undefined
        payload.password = state.password.trim() || undefined
      } else {
        payload.secret = state.secret.trim() || undefined
      }
      await onAdd(payload)
      onClose()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to add service')
    } finally { setSubmitting(false) }
  }

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal" onClick={e => e.stopPropagation()}>
        <header className="modal-header">
          <h2>Add Service</h2>
          <button className="modal-close" onClick={onClose}>&times;</button>
        </header>
        <form onSubmit={handleSubmit} className="modal-body">
          <div className="form-section">
            <h3>Choose a Template</h3>
            <div className="template-grid">
              <button type="button"
                className={`template-card ${selectedTemplate === '' ? 'template-card--selected' : ''}`}
                onClick={() => setSelectedTemplate('')}>
                <span>Blank</span>
              </button>
              {templates.map(t => (
                <button key={t.slug} type="button"
                  className={`template-card ${selectedTemplate === t.slug ? 'template-card--selected' : ''}`}
                  onClick={() => setSelectedTemplate(t.slug)}>
                  <img src={`/icons/${t.icon}`} alt="" className="template-icon" />
                  <span>{t.name}</span>
                </button>
              ))}
            </div>
          </div>

          <ServiceFormFields state={state} update={update} mode="add" />

          {error && <div className="form-error">{error}</div>}
          <footer className="modal-footer">
            <button type="button" className="btn-secondary" onClick={onClose}>Cancel</button>
            <button type="submit" className="btn-primary" disabled={submitting}>
              {submitting ? 'Adding...' : 'Add Service'}
            </button>
          </footer>
        </form>
      </div>
    </div>
  )
}
