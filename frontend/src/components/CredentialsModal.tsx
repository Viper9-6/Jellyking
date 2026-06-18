import { useState } from 'react'
import type { ServiceStatus } from '../types'
import { updateService } from '../api/services'
import './AddServiceModal.css'

interface Props {
  service: ServiceStatus
  onClose: () => void
  onSaved: () => void
}

export function CredentialsModal({ service, onClose, onSaved }: Props) {
  const [authType, setAuthType] = useState<ServiceStatus['authType']>(service.authType ?? 'none')
  const [secret, setSecret] = useState('')
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [done, setDone] = useState(false)

  const submit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      // undefined fields = unchanged; empty-string secret = clear.
      const payload: Parameters<typeof updateService>[1] = { authType }
      if (authType === 'none') {
        payload.secret = '' // clear stored credential
      } else if (authType === 'qbittorrent') {
        payload.username = username.trim() || undefined
        payload.password = password.trim() || undefined
      } else {
        payload.secret = secret.length ? secret : undefined
      }
      await updateService(service.serviceId, payload)
      setDone(true)
      onSaved()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save credentials')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal" onClick={e => e.stopPropagation()}>
        <header className="modal-header">
          <h2>Auto-login — {service.name}</h2>
          <button className="modal-close" onClick={onClose}>&times;</button>
        </header>

        {done ? (
          <div className="modal-body">
            <p className="form-success">Credentials saved. The WebUI will load signed in.</p>
            <footer className="modal-footer">
              <button className="btn-primary" onClick={onClose}>Done</button>
            </footer>
          </div>
        ) : (
          <form onSubmit={submit} className="modal-body">
            <div className="form-section">
              <p className="form-hint">
                Jellyking stores this credential and injects it on every proxied request
                so the service opens already authenticated. Secrets are encrypted at rest
                and never shown again after saving.
              </p>
              <div className="form-grid">
                <div className="form-field">
                  <label htmlFor="cm-auth">Authentication</label>
                  <select id="cm-auth" value={authType}
                    onChange={e => setAuthType(e.target.value as ServiceStatus['authType'])}>
                    <option value="none">None</option>
                    <option value="apikey">API Key (X-Api-Key)</option>
                    <option value="jellyfin">Jellyfin Token (X-Emby-Token)</option>
                    <option value="qbittorrent">qBittorrent (username + password)</option>
                  </select>
                </div>
                {(authType === 'apikey' || authType === 'jellyfin') && (
                  <div className="form-field">
                    <label htmlFor="cm-secret">Secret</label>
                    <input id="cm-secret" type="password" value={secret}
                      onChange={e => setSecret(e.target.value)} placeholder="Leave blank to keep existing"
                      autoComplete="off" />
                  </div>
                )}
                {authType === 'qbittorrent' && (
                  <>
                    <div className="form-field">
                      <label htmlFor="cm-user">Username</label>
                      <input id="cm-user" type="text" value={username}
                        onChange={e => setUsername(e.target.value)} placeholder="Leave blank to keep existing"
                        autoComplete="off" />
                    </div>
                    <div className="form-field">
                      <label htmlFor="cm-pass">Password</label>
                      <input id="cm-pass" type="password" value={password}
                        onChange={e => setPassword(e.target.value)} placeholder="Leave blank to keep existing"
                        autoComplete="off" />
                    </div>
                  </>
                )}
              </div>
            </div>

            {error && <div className="form-error">{error}</div>}

            <footer className="modal-footer">
              <button type="button" className="btn-secondary" onClick={onClose}>Cancel</button>
              <button type="submit" className="btn-primary" disabled={submitting}>
                {submitting ? 'Saving…' : 'Save'}
              </button>
            </footer>
          </form>
        )}
      </div>
    </div>
  )
}
