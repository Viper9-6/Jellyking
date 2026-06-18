import { useState, useEffect } from 'react'
import type { Service } from '../types'
import type { CreateServicePayload } from '../api/services'
import { fetchTemplates } from '../api/services'
import './AddServiceModal.css'

interface Props {
  onClose: () => void
  onAdd: (service: CreateServicePayload) => Promise<void>
}

const emptyService: Omit<Service, 'id'> = {
  slug: '',
  name: '',
  host: 'localhost',
  port: 8080,
  basePath: '',
  healthPath: '',
  icon: '',
  webSocketPaths: '',
  priority: 100,
  enabled: true,
  authType: 'none',
}

export function AddServiceModal({ onClose, onAdd }: Props) {
  const [templates, setTemplates] = useState<Service[]>([])
  const [selectedTemplate, setSelectedTemplate] = useState<string>('')
  const [service, setService] = useState<Omit<Service, 'id'>>(emptyService)
  const [secret, setSecret] = useState('')
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    fetchTemplates().then(setTemplates).catch(console.error)
  }, [])

  useEffect(() => {
    if (selectedTemplate) {
      const template = templates.find(t => t.slug === selectedTemplate)
      if (template) {
        setService({
          slug: template.slug,
          name: template.name,
          host: template.host,
          port: template.port,
          basePath: template.basePath,
          healthPath: template.healthPath,
          icon: template.icon,
          webSocketPaths: template.webSocketPaths,
          priority: template.priority,
          enabled: template.enabled,
          authType: template.authType ?? 'none',
        })
        setSecret('')
        setUsername('')
        setPassword('')
      }
    }
  }, [selectedTemplate, templates])

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      const payload: Parameters<typeof onAdd>[0] = { ...service }
      if (service.authType === 'qbittorrent') {
        payload.username = username.trim() || undefined
        payload.password = password.trim() || undefined
      } else {
        payload.secret = secret.trim() || undefined
      }
      await onAdd(payload)
      onClose()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to add service')
    } finally {
      setSubmitting(false)
    }
  }

  const updateField = <K extends keyof Omit<Service, 'id'>>(field: K, value: Omit<Service, 'id'>[K]) => {
    setService(prev => ({ ...prev, [field]: value }))
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
              <button
                type="button"
                className={`template-card ${selectedTemplate === '' ? 'template-card--selected' : ''}`}
                onClick={() => setSelectedTemplate('')}
              >
                <span>Blank</span>
              </button>
              {templates.map(t => (
                <button
                  key={t.slug}
                  type="button"
                  className={`template-card ${selectedTemplate === t.slug ? 'template-card--selected' : ''}`}
                  onClick={() => setSelectedTemplate(t.slug)}
                >
                  <img src={`/icons/${t.icon}`} alt="" className="template-icon" />
                  <span>{t.name}</span>
                </button>
              ))}
            </div>
          </div>

          <div className="form-section">
            <h3>Service Details</h3>
            <div className="form-grid">
              <div className="form-field">
                <label htmlFor="slug">Slug</label>
                <input id="slug" type="text" value={service.slug}
                  onChange={e => updateField('slug', e.target.value)} required placeholder="e.g., sonarr" />
              </div>
              <div className="form-field">
                <label htmlFor="name">Name</label>
                <input id="name" type="text" value={service.name}
                  onChange={e => updateField('name', e.target.value)} required placeholder="e.g., Sonarr" />
              </div>
              <div className="form-field">
                <label htmlFor="host">Host</label>
                <input id="host" type="text" value={service.host}
                  onChange={e => updateField('host', e.target.value)} required placeholder="e.g., localhost" />
              </div>
              <div className="form-field">
                <label htmlFor="port">Port</label>
                <input id="port" type="number" value={service.port}
                  onChange={e => updateField('port', parseInt(e.target.value) || 0)} required min={1} max={65535} />
              </div>
              <div className="form-field">
                <label htmlFor="basePath">Base Path</label>
                <input id="basePath" type="text" value={service.basePath}
                  onChange={e => updateField('basePath', e.target.value)} required placeholder="e.g., /sonarr" />
              </div>
              <div className="form-field">
                <label htmlFor="healthPath">Health Path</label>
                <input id="healthPath" type="text" value={service.healthPath}
                  onChange={e => updateField('healthPath', e.target.value)} placeholder="e.g., /sonarr/api/v3/system/status" />
              </div>
              <div className="form-field">
                <label htmlFor="icon">Icon</label>
                <input id="icon" type="text" value={service.icon}
                  onChange={e => updateField('icon', e.target.value)} placeholder="e.g., sonarr.svg" />
              </div>
              <div className="form-field">
                <label htmlFor="priority">Priority</label>
                <input id="priority" type="number" value={service.priority}
                  onChange={e => updateField('priority', parseInt(e.target.value) || 0)} required min={0} />
              </div>
              <div className="form-field">
                <label htmlFor="webSocketPaths">WebSocket Paths</label>
                <input id="webSocketPaths" type="text" value={service.webSocketPaths}
                  onChange={e => updateField('webSocketPaths', e.target.value)} placeholder="e.g., /sonarr/signalr" />
              </div>
              <div className="form-field form-field--checkbox">
                <label>
                  <input type="checkbox" checked={service.enabled}
                    onChange={e => updateField('enabled', e.target.checked)} />
                  Enabled
                </label>
              </div>
            </div>
          </div>

          <div className="form-section">
            <h3>Auto-login (optional)</h3>
            <p className="form-hint">
              Store a credential so Jellyking logs you into this service automatically.
              Secrets are encrypted at rest. API Key covers Sonarr/Radarr/Prowlarr/Lidarr/
              Readarr/Bazarr/Jellyseerr/SABnzbd; Jellyfin Token uses a Jellyfin API key;
              qBittorrent does a server-side login with your username + password.
            </p>
            <div className="form-grid">
              <div className="form-field">
                <label htmlFor="authType">Authentication</label>
                <select id="authType" value={service.authType}
                  onChange={e => updateField('authType', e.target.value as Service['authType'])}>
                  <option value="none">None</option>
                  <option value="apikey">API Key (X-Api-Key)</option>
                  <option value="jellyfin">Jellyfin Token (X-Emby-Token)</option>
                  <option value="qbittorrent">qBittorrent (username + password)</option>
                </select>
              </div>
              {(service.authType === 'apikey' || service.authType === 'jellyfin') && (
                <div className="form-field">
                  <label htmlFor="secret">Secret</label>
                  <input id="secret" type="password" value={secret}
                    onChange={e => setSecret(e.target.value)} placeholder="API key or token"
                    autoComplete="off" />
                </div>
              )}
              {service.authType === 'qbittorrent' && (
                <>
                  <div className="form-field">
                    <label htmlFor="qbit-user">Username</label>
                    <input id="qbit-user" type="text" value={username}
                      onChange={e => setUsername(e.target.value)} placeholder="qBittorrent username"
                      autoComplete="off" />
                  </div>
                  <div className="form-field">
                    <label htmlFor="qbit-pass">Password</label>
                    <input id="qbit-pass" type="password" value={password}
                      onChange={e => setPassword(e.target.value)} autoComplete="off" />
                  </div>
                </>
              )}
            </div>
          </div>

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
