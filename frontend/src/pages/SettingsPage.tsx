import { useCallback, useEffect, useState } from 'react'
import type { SettingsDto, UpdateSettingsRequest, UserDto } from '../types'
import { updateSettings } from '../api/settings'
import { fetchUsers, createUser, updateUser, deleteUser } from '../api/users'
import './SettingsPage.css'

interface Props {
  settings: SettingsDto | null
  onSettingsChanged: () => void
}

export function SettingsPage({ settings, onSettingsChanged }: Props) {
  const [title, setTitle] = useState(settings?.title ?? 'Jellyking')
  const [theme, setTheme] = useState(settings?.theme ?? 'dark')
  const [localAccess, setLocalAccess] = useState(settings?.localAccessEnabled ?? false)
  const [saving, setSaving] = useState(false)
  const [msg, setMsg] = useState<{ kind: 'ok' | 'err'; text: string } | null>(null)

  const [users, setUsers] = useState<UserDto[]>([])
  const [usersLoading, setUsersLoading] = useState(true)
  const [showAddUser, setShowAddUser] = useState(false)
  const [resetTarget, setResetTarget] = useState<UserDto | null>(null)

  const reloadUsers = useCallback(async () => {
    try { setUsers(await fetchUsers()) } catch { /* ignore */ } finally { setUsersLoading(false) }
  }, [])

  useEffect(() => { reloadUsers() }, [reloadUsers])

  useEffect(() => {
    setTitle(settings?.title ?? 'Jellyking')
    setTheme(settings?.theme ?? 'dark')
    setLocalAccess(settings?.localAccessEnabled ?? false)
  }, [settings])

  const save = async () => {
    setSaving(true); setMsg(null)
    try {
      const req: UpdateSettingsRequest = { title, theme, localAccessEnabled: localAccess }
      await updateSettings(req)
      await onSettingsChanged()
      setMsg({ kind: 'ok', text: 'Settings saved.' })
    } catch (err) {
      setMsg({ kind: 'err', text: err instanceof Error ? err.message : 'Save failed' })
    } finally { setSaving(false) }
  }

  const handleDeleteUser = async (u: UserDto) => {
    if (!confirm(`Delete user "${u.username}"? This cannot be undone.`)) return
    try {
      await deleteUser(u.id)
      await reloadUsers()
    } catch (err) {
      alert(err instanceof Error ? err.message : 'Delete failed')
    }
  }

  const handleRoleChange = async (u: UserDto, role: 'Admin' | 'User') => {
    try { await updateUser(u.id, { role }); await reloadUsers() }
    catch (err) { alert(err instanceof Error ? err.message : 'Update failed') }
  }

  return (
    <div className="settings">
      <section className="settings__section">
        <h2 className="settings__heading">General</h2>

        <div className="settings__row">
          <label className="settings__label" htmlFor="title">Dashboard title</label>
          <input id="title" className="settings__input" value={title}
            onChange={e => setTitle(e.target.value)} />
        </div>

        <div className="settings__row">
          <span className="settings__label">Theme</span>
          <div className="segmented">
            <button type="button" className={theme === 'dark' ? 'segmented__btn segmented__btn--active' : 'segmented__btn'}
              onClick={() => setTheme('dark')}>Dark</button>
            <button type="button" className={theme === 'light' ? 'segmented__btn segmented__btn--active' : 'segmented__btn'}
              onClick={() => setTheme('light')}>Light</button>
          </div>
        </div>

        <div className="settings__row">
          <div>
            <span className="settings__label">Local access (no login)</span>
            <p className="settings__hint">
              When enabled, requests from localhost are signed in as admin
              without a password. Convenient for a single-user machine.
            </p>
          </div>
          <button
            type="button"
            className={localAccess ? 'toggle toggle--on' : 'toggle'}
            role="switch"
            aria-checked={localAccess}
            onClick={() => setLocalAccess(v => !v)}
          >
            <span className="toggle__knob" />
          </button>
        </div>

        <div className="settings__actions">
          <button className="btn-primary" onClick={save} disabled={saving}>
            {saving ? 'Saving…' : 'Save settings'}
          </button>
          {msg && <span className={msg.kind === 'ok' ? 'settings__msg settings__msg--ok' : 'settings__msg settings__msg--err'}>{msg.text}</span>}
        </div>
      </section>

      <section className="settings__section">
        <div className="settings__section-head">
          <h2 className="settings__heading">Users</h2>
          <button className="btn-primary" onClick={() => setShowAddUser(true)}>+ Add user</button>
        </div>

        {usersLoading ? (
          <p className="settings__hint">Loading…</p>
        ) : (
          <table className="users-table">
            <thead>
              <tr><th>Username</th><th>Role</th><th>Created</th><th></th></tr>
            </thead>
            <tbody>
              {users.map(u => (
                <tr key={u.id}>
                  <td className="users-table__name">{u.username}</td>
                  <td>
                    <select value={u.role} onChange={e => handleRoleChange(u, e.target.value as 'Admin' | 'User')}>
                      <option value="Admin">Admin</option>
                      <option value="User">User</option>
                    </select>
                  </td>
                  <td className="users-table__date">{new Date(u.createdAt).toLocaleDateString()}</td>
                  <td className="users-table__actions">
                    <button className="btn-secondary btn-sm" onClick={() => setResetTarget(u)}>Reset password</button>
                    <button className="btn-danger btn-sm" onClick={() => handleDeleteUser(u)}>Delete</button>
                  </td>
                </tr>
              ))}
              {users.length === 0 && (
                <tr><td colSpan={4} className="users-table__empty">No users yet.</td></tr>
              )}
            </tbody>
          </table>
        )}
      </section>

      {showAddUser && (
        <AddUserModal onClose={() => setShowAddUser(false)} onCreated={reloadUsers} />
      )}
      {resetTarget && (
        <ResetPasswordModal user={resetTarget} onClose={() => setResetTarget(null)} />
      )}
    </div>
  )
}

function AddUserModal({ onClose, onCreated }: { onClose: () => void; onCreated: () => void }) {
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [role, setRole] = useState<'Admin' | 'User'>('User')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const submit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    if (password.length < 6) { setError('Password must be at least 6 characters'); return }
    setSubmitting(true)
    try { await createUser({ username, password, role }); await onCreated(); onClose() }
    catch (err) { setError(err instanceof Error ? err.message : 'Failed to create user') }
    finally { setSubmitting(false) }
  }

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal" onClick={e => e.stopPropagation()}>
        <header className="modal-header"><h2>Add user</h2><button className="modal-close" onClick={onClose}>&times;</button></header>
        <form onSubmit={submit} className="modal-body">
          <div className="form-section">
            <div className="form-grid">
              <div className="form-field">
                <label htmlFor="au-user">Username</label>
                <input id="au-user" value={username} onChange={e => setUsername(e.target.value)} required disabled={submitting} />
              </div>
              <div className="form-field">
                <label htmlFor="au-pass">Password</label>
                <input id="au-pass" type="password" value={password} onChange={e => setPassword(e.target.value)} required disabled={submitting} />
              </div>
              <div className="form-field">
                <label htmlFor="au-role">Role</label>
                <select id="au-role" value={role} onChange={e => setRole(e.target.value as 'Admin' | 'User')} disabled={submitting}>
                  <option value="User">User</option>
                  <option value="Admin">Admin</option>
                </select>
              </div>
            </div>
          </div>
          {error && <div className="form-error">{error}</div>}
          <footer className="modal-footer">
            <button type="button" className="btn-secondary" onClick={onClose}>Cancel</button>
            <button type="submit" className="btn-primary" disabled={submitting}>{submitting ? 'Creating…' : 'Create user'}</button>
          </footer>
        </form>
      </div>
    </div>
  )
}

function ResetPasswordModal({ user, onClose }: { user: UserDto; onClose: () => void }) {
  const [password, setPassword] = useState('')
  const [confirm, setConfirm] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const [done, setDone] = useState(false)

  const submit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    if (password.length < 6) { setError('Password must be at least 6 characters'); return }
    if (password !== confirm) { setError('Passwords do not match'); return }
    setSubmitting(true)
    try { await updateUser(user.id, { password }); setDone(true) }
    catch (err) { setError(err instanceof Error ? err.message : 'Failed to reset password') }
    finally { setSubmitting(false) }
  }

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal" onClick={e => e.stopPropagation()}>
        <header className="modal-header"><h2>Reset password — {user.username}</h2><button className="modal-close" onClick={onClose}>&times;</button></header>
        {done ? (
          <div className="modal-body">
            <p className="form-success">Password updated for {user.username}.</p>
            <footer className="modal-footer"><button className="btn-primary" onClick={onClose}>Done</button></footer>
          </div>
        ) : (
          <form onSubmit={submit} className="modal-body">
            <div className="form-section">
              <div className="form-grid">
                <div className="form-field">
                  <label htmlFor="rp-pass">New password</label>
                  <input id="rp-pass" type="password" value={password} onChange={e => setPassword(e.target.value)} required disabled={submitting} />
                </div>
                <div className="form-field">
                  <label htmlFor="rp-conf">Confirm password</label>
                  <input id="rp-conf" type="password" value={confirm} onChange={e => setConfirm(e.target.value)} required disabled={submitting} />
                </div>
              </div>
            </div>
            {error && <div className="form-error">{error}</div>}
            <footer className="modal-footer">
              <button type="button" className="btn-secondary" onClick={onClose}>Cancel</button>
              <button type="submit" className="btn-primary" disabled={submitting}>{submitting ? 'Saving…' : 'Reset password'}</button>
            </footer>
          </form>
        )}
      </div>
    </div>
  )
}
