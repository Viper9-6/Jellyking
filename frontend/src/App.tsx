import { useCallback, useEffect, useState } from 'react'
import { useAuth } from './context/AuthContext'
import { useServices } from './hooks/useServices'
import { ServiceGrid } from './components/ServiceGrid'
import { AddServiceModal } from './components/AddServiceModal'
import { ChangePasswordModal } from './components/ChangePasswordModal'
import { CredentialsModal } from './components/CredentialsModal'
import { EditServiceModal } from './components/EditServiceModal'
import { SettingsPage } from './pages/SettingsPage'
import { SetupPage } from './pages/SetupPage'
import { LoginPage } from './pages/LoginPage'
import { getSettings } from './api/settings'
import type { SettingsDto, ServiceStatus } from './types'
import { createService, deleteService } from './api/services'
import './App.css'

type View = 'dashboard' | 'settings'

export default function App() {
  const { user, loading, isSetupRequired, logout } = useAuth()
  const { services, loading: servicesLoading, error, refresh } = useServices()
  const [view, setView] = useState<View>('dashboard')
  const [showAddService, setShowAddService] = useState(false)
  const [showChangePassword, setShowChangePassword] = useState(false)
  const [credSvc, setCredSvc] = useState<ServiceStatus | null>(null)
  const [editSvc, setEditSvc] = useState<ServiceStatus | null>(null)
  const [settings, setSettings] = useState<SettingsDto | null>(null)
  const [now, setNow] = useState(new Date())

  const loadSettings = useCallback(async () => {
    try { setSettings(await getSettings()) } catch { /* ignore */ }
  }, [])

  useEffect(() => { loadSettings() }, [loadSettings])

  useEffect(() => {
    const root = document.documentElement
    root.setAttribute('data-theme', settings?.theme ?? 'dark')
  }, [settings?.theme])

  useEffect(() => {
    const id = setInterval(() => setNow(new Date()), 10_000)
    return () => clearInterval(id)
  }, [])

  if (loading) {
    return (
      <div className="splash">
        <div className="splash__spinner" />
        <p className="splash__text">Loading Jellyking…</p>
      </div>
    )
  }

  if (isSetupRequired) return <SetupGate />
  if (!user) return <LoginGate />

  const isAdmin = user.role === 'Admin'
  const title = settings?.title ?? 'Jellyking'

  const handleAddService = async (svc: Parameters<typeof createService>[0]) => {
    await createService(svc)
    await refresh()
  }

  const handleDeleteService = async (id: string) => {
    await deleteService(id)
    await refresh()
  }

  return (
    <div className="layout">
      <header className="header">
        <div className="header__brand">
          <img src="/icons/jellyking.svg" alt="" className="header__logo" />
          <span className="header__title">{title}</span>
        </div>
        <div className="header__actions">
          <span className="header__status">
            {services.filter(s => s.isUp).length} / {services.length} online
          </span>
          <nav className="header__nav">
            <button
              className={`header__tab ${view === 'dashboard' ? 'header__tab--active' : ''}`}
              onClick={() => setView('dashboard')}
              title="Dashboard"
            >
              Dashboard
            </button>
            {isAdmin && (
              <button
                className={`header__tab ${view === 'settings' ? 'header__tab--active' : ''}`}
                onClick={() => setView('settings')}
                title="Admin settings"
              >
                Settings
              </button>
            )}
          </nav>
          <button className="header__refresh" onClick={refresh} title="Refresh services">
            ↻
          </button>
          <div className="header__user">
            <span className="header__username">{user.username}</span>
            <button className="header__link" onClick={() => setShowChangePassword(true)}>
              Change password
            </button>
            <button className="header__link header__link--muted" onClick={logout}>
              Sign out
            </button>
          </div>
        </div>
      </header>

      <main className="main">
        {view === 'settings' && isAdmin ? (
          <SettingsPage settings={settings} onSettingsChanged={loadSettings} />
        ) : (
          <>
            {isAdmin && (
              <div className="dashboard__toolbar">
                <button className="btn-primary" onClick={() => setShowAddService(true)}>
                  + Add Service
                </button>
              </div>
            )}

            {servicesLoading && services.length === 0 ? (
              <div className="splash">
                <div className="splash__spinner" />
                <p className="splash__text">Detecting services…</p>
              </div>
            ) : error ? (
              <div className="splash splash--error">
                <p className="splash__text">Could not reach Jellyking backend</p>
                <code className="splash__code">{error}</code>
              </div>
            ) : services.length === 0 ? (
              <div className="splash">
                <p className="splash__text">No services configured yet.</p>
                {isAdmin && (
                  <button className="btn-primary" onClick={() => setShowAddService(true)}>
                    Add your first service
                  </button>
                )}
              </div>
            ) : (
              <ServiceGrid
              services={services}
              now={now}
              onDelete={isAdmin ? handleDeleteService : undefined}
              onEdit={isAdmin ? (svc) => setEditSvc(svc) : undefined}
              onEditCredentials={isAdmin ? (svc) => setCredSvc(svc) : undefined}
            />
            )}
          </>
        )}
      </main>

      {showAddService && (
        <AddServiceModal
          onClose={() => setShowAddService(false)}
          onAdd={handleAddService}
        />
      )}

      {showChangePassword && (
        <ChangePasswordModal onClose={() => setShowChangePassword(false)} />
      )}

      {credSvc && (
        <CredentialsModal
          service={credSvc}
          onClose={() => setCredSvc(null)}
          onSaved={refresh}
        />
      )}

      {editSvc && (
        <EditServiceModal
          serviceId={editSvc.serviceId}
          onClose={() => setEditSvc(null)}
          onSaved={refresh}
        />
      )}
    </div>
  )
}

function SetupGate() {
  return <SetupPage />
}

function LoginGate() {
  return <LoginPage />
}
