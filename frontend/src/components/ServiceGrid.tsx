import type { ServiceStatus } from '../types'
import './ServiceGrid.css'

interface Props {
  services: ServiceStatus[]
  now: Date
  onDelete?: (id: string) => void
  onEdit?: (svc: ServiceStatus) => void
  onEditCredentials?: (svc: ServiceStatus) => void
}

function timeSince(iso: string, now: Date): string {
  const then = new Date(iso).getTime()
  const seconds = Math.max(0, Math.floor((now.getTime() - then) / 1000))
  if (seconds < 60) return `${seconds}s ago`
  if (seconds < 3600) return `${Math.floor(seconds / 60)}m ago`
  return `${Math.floor(seconds / 3600)}h ago`
}

export function ServiceGrid({ services, now, onDelete, onEdit, onEditCredentials }: Props) {
  return (
    <div className="grid">
      {services.map(svc => {
        const href = `${svc.basePath}/`
        return (
          <article key={svc.id} className={`card ${svc.isUp ? '' : 'card--offline'}`}>
            <div className="card__top">
              <div className="card__icon-wrap">
                <img
                  src={`/icons/${svc.icon}`}
                  alt={svc.name}
                  className="card__icon"
                  onError={e => { (e.currentTarget as HTMLImageElement).style.display = 'none' }}
                />
              </div>
              {(onDelete || onEdit || onEditCredentials) && (
                <div className="card__actions">
                  {onEdit && (
                    <button
                      className="card__iconbtn"
                      title="Edit service"
                      onClick={() => onEdit(svc)}
                    >
                      <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M12 20h9" /><path d="M16.5 3.5a2.121 2.121 0 0 1 3 3L7 19l-4 1 1-4Z" /></svg>
                    </button>
                  )}
                  {onEditCredentials && (
                    <button
                      className="card__iconbtn"
                      title="Auto-login credentials"
                      onClick={() => onEditCredentials(svc)}
                    >
                      <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M21 2l-2 2m-7.61 7.61a5.5 5.5 0 1 1-7.778 7.778 5.5 5.5 0 0 1 7.778-7.778zm0 0L15.5 7.5m0 0l3 3L22 7l-3-3m-3.5 3.5L19 4" /></svg>
                    </button>
                  )}
                  {onDelete && (
                    <button
                      className="card__iconbtn"
                      title={`Remove ${svc.name}`}
                      onClick={() => onDelete(svc.id)}
                    >
                      ×
                    </button>
                  )}
                </div>
              )}
            </div>

            <div className="card__body">
              <h3 className="card__title">
                {svc.name}
                <span
                  className={`card__dot card__dot--${svc.isUp ? 'up' : 'down'}`}
                  aria-label={svc.isUp ? 'online' : 'offline'}
                />
                {svc.authType && svc.authType !== 'none' && (
                  <span className="card__authtag" title={`Auto-login: ${svc.authType}`}>auto</span>
                )}
              </h3>
              <p className="card__path">{svc.basePath}</p>
              <p className="card__endpoint">{svc.host}:{svc.port}</p>
              {svc.downReason && <p className="card__reason">{svc.downReason}</p>}
            </div>

            <a
              href={href}
              target="_blank"
              rel="noopener noreferrer"
              className={`card__action ${svc.isUp ? '' : 'card__action--disabled'}`}
              aria-disabled={!svc.isUp}
              tabIndex={svc.isUp ? 0 : -1}
              onClick={e => !svc.isUp && e.preventDefault()}
            >
              Open
            </a>

            <span className="card__checked">Checked {timeSince(svc.lastChecked, now)}</span>
          </article>
        )
      })}
    </div>
  )
}
