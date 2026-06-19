import { useState } from 'react'
import type { ServiceFormState, ServiceTestResult } from '../types'
import { testService } from '../api/services'
import './AddServiceModal.css'

interface Props {
  state: ServiceFormState
  update: (patch: Partial<ServiceFormState>) => void
  /** 'edit' shows "leave blank to keep existing" hints on credential fields. */
  mode?: 'add' | 'edit'
}

export function ServiceFormFields({ state, update, mode = 'add' }: Props) {
  const [testing, setTesting] = useState(false)
  const [result, setResult] = useState<ServiceTestResult | null>(null)
  const [testErr, setTestErr] = useState<string | null>(null)

  const runTest = async () => {
    setTesting(true); setResult(null); setTestErr(null)
    try {
      setResult(await testService({
        host: state.host, port: state.port,
        basePath: state.basePath, healthPath: state.healthPath,
      }))
    } catch (e) {
      setTestErr(e instanceof Error ? e.message : 'Test failed')
    } finally { setTesting(false) }
  }

  const keepHint = mode === 'edit' ? ' (leave blank to keep existing)' : ''

  return (
    <>
      <div className="form-section">
        <h3>Service Details</h3>
        <div className="form-grid">
          <div className="form-field">
            <label htmlFor="slug">Slug</label>
            <input id="slug" type="text" value={state.slug}
              onChange={e => update({ slug: e.target.value })} required placeholder="e.g., sonarr" />
          </div>
          <div className="form-field">
            <label htmlFor="name">Name</label>
            <input id="name" type="text" value={state.name}
              onChange={e => update({ name: e.target.value })} required placeholder="e.g., Sonarr" />
          </div>
          <div className="form-field">
            <label htmlFor="host">Host</label>
            <input id="host" type="text" value={state.host}
              onChange={e => update({ host: e.target.value })} required placeholder="e.g., localhost" />
          </div>
          <div className="form-field">
            <label htmlFor="port">Port</label>
            <input id="port" type="number" value={state.port}
              onChange={e => update({ port: parseInt(e.target.value) || 0 })} required min={1} max={65535} />
          </div>
          <div className="form-field">
            <label htmlFor="basePath">Base Path <span className="settings__hint" style={{ fontWeight: 400 }}>(optional)</span></label>
            <input id="basePath" type="text" value={state.basePath}
              onChange={e => update({ basePath: e.target.value })} placeholder="blank = /slug, e.g. /sonarr" />
          </div>
          <div className="form-field">
            <label htmlFor="healthPath">Health Path</label>
            <input id="healthPath" type="text" value={state.healthPath}
              onChange={e => update({ healthPath: e.target.value })} placeholder="e.g., /sonarr/api/v3/system/status" />
          </div>
          <div className="form-field">
            <label htmlFor="icon">Icon</label>
            <input id="icon" type="text" value={state.icon}
              onChange={e => update({ icon: e.target.value })} placeholder="e.g., sonarr.svg" />
          </div>
          <div className="form-field">
            <label htmlFor="priority">Priority</label>
            <input id="priority" type="number" value={state.priority}
              onChange={e => update({ priority: parseInt(e.target.value) || 0 })} required min={0} />
          </div>
          <div className="form-field">
            <label htmlFor="webSocketPaths">WebSocket Paths</label>
            <input id="webSocketPaths" type="text" value={state.webSocketPaths}
              onChange={e => update({ webSocketPaths: e.target.value })} placeholder="e.g., /sonarr/signalr" />
          </div>
          <div className="form-field form-field--checkbox">
            <label>
              <input type="checkbox" checked={state.enabled}
                onChange={e => update({ enabled: e.target.checked })} />
              Enabled
            </label>
          </div>
        </div>

        <div className="settings__row" style={{ borderTop: '1px solid var(--color-border)', marginTop: 12, paddingTop: 12 }}>
          <div>
            <span className="settings__label">Test connection</span>
            <p className="settings__hint">
              Probes {state.host || 'host'}:{state.port || 'port'}{state.healthPath || '/'} and reports whether the
              upstream is reachable. A 404 here usually means the app's URL Base hasn't been
              set to match <code>{state.basePath || '/slug'}</code> — set it on the app and restart.
            </p>
          </div>
          <button type="button" className="btn-secondary" onClick={runTest} disabled={testing}>
            {testing ? 'Testing…' : 'Test'}
          </button>
        </div>
        {testErr && <div className="form-error">{testErr}</div>}
        {result && (
          <div className={result.reachable && (result.httpStatus ?? 0) < 400 ? 'form-success' : 'form-error'}>
            <strong>HTTP {result.httpStatus ?? '—'}</strong> — {result.hint}
          </div>
        )}
      </div>

      <div className="form-section">
        <h3>Auto-login (optional)</h3>
        <p className="form-hint">
          Credentials authenticate you to the service; they don't fix routing — the app
          still needs its URL Base set to the Base Path above. Secrets are encrypted at rest.
        </p>
        <div className="form-grid">
          <div className="form-field">
            <label htmlFor="authType">Authentication</label>
            <select id="authType" value={state.authType}
              onChange={e => update({ authType: e.target.value as ServiceFormState['authType'] })}>
              <option value="none">None</option>
              <option value="apikey">API Key (X-Api-Key)</option>
              <option value="jellyfin">Jellyfin Token (X-Emby-Token)</option>
              <option value="qbittorrent">qBittorrent (username + password)</option>
            </select>
          </div>
          {(state.authType === 'apikey' || state.authType === 'jellyfin') && (
            <div className="form-field">
              <label htmlFor="secret">Secret{keepHint}</label>
              <input id="secret" type="password" value={state.secret}
                onChange={e => update({ secret: e.target.value })}
                placeholder={mode === 'edit' ? 'Leave blank to keep existing' : 'API key or token'}
                autoComplete="off" />
            </div>
          )}
          {state.authType === 'qbittorrent' && (
            <>
              <div className="form-field">
                <label htmlFor="qbit-user">Username{keepHint}</label>
                <input id="qbit-user" type="text" value={state.username}
                  onChange={e => update({ username: e.target.value })}
                  placeholder={mode === 'edit' ? 'Leave blank to keep existing' : 'qBittorrent username'}
                  autoComplete="off" />
              </div>
              <div className="form-field">
                <label htmlFor="qbit-pass">Password{keepHint}</label>
                <input id="qbit-pass" type="password" value={state.password}
                  onChange={e => update({ password: e.target.value })} autoComplete="off" />
              </div>
            </>
          )}
        </div>
      </div>
    </>
  )
}
