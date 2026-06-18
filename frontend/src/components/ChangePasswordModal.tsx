import { useState } from 'react'
import { changePassword } from '../api/changePassword'
import './AddServiceModal.css'

interface Props {
  onClose: () => void
}

export function ChangePasswordModal({ onClose }: Props) {
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirm, setConfirm] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const [done, setDone] = useState(false)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)

    if (newPassword.length < 6) {
      setError('New password must be at least 6 characters')
      return
    }
    if (newPassword !== confirm) {
      setError('New passwords do not match')
      return
    }

    setSubmitting(true)
    try {
      await changePassword(currentPassword, newPassword)
      setDone(true)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to change password')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal" onClick={e => e.stopPropagation()}>
        <header className="modal-header">
          <h2>Change Password</h2>
          <button className="modal-close" onClick={onClose}>&times;</button>
        </header>

        {done ? (
          <div className="modal-body">
            <p className="form-success">Your password has been updated.</p>
            <footer className="modal-footer">
              <button type="button" className="btn-primary" onClick={onClose}>Done</button>
            </footer>
          </div>
        ) : (
          <form onSubmit={handleSubmit} className="modal-body">
            <div className="form-section">
              <div className="form-grid">
                <div className="form-field">
                  <label htmlFor="current">Current password</label>
                  <input id="current" type="password" value={currentPassword}
                    onChange={e => setCurrentPassword(e.target.value)} required
                    autoComplete="current-password" disabled={submitting} />
                </div>
                <div className="form-field">
                  <label htmlFor="new">New password</label>
                  <input id="new" type="password" value={newPassword}
                    onChange={e => setNewPassword(e.target.value)} required
                    autoComplete="new-password" disabled={submitting} />
                </div>
                <div className="form-field">
                  <label htmlFor="confirm">Confirm new password</label>
                  <input id="confirm" type="password" value={confirm}
                    onChange={e => setConfirm(e.target.value)} required
                    autoComplete="new-password" disabled={submitting} />
                </div>
              </div>
            </div>

            {error && <div className="form-error">{error}</div>}

            <footer className="modal-footer">
              <button type="button" className="btn-secondary" onClick={onClose}>Cancel</button>
              <button type="submit" className="btn-primary" disabled={submitting}>
                {submitting ? 'Saving…' : 'Update password'}
              </button>
            </footer>
          </form>
        )}
      </div>
    </div>
  )
}
