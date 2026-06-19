import { useEffect, useRef, useState } from 'react'
import type { ServiceStatus } from '../types'
import './ServiceFrame.css'

interface Props {
  service: ServiceStatus
  onBack: () => void
}

export function ServiceFrame({ service, onBack }: Props) {
  const [loading, setLoading] = useState(true)
  const iframeRef = useRef<HTMLIFrameElement>(null)
  const src = `${service.basePath}/`

  // Reset the loading state whenever the service changes.
  useEffect(() => { setLoading(true) }, [service.id])

  return (
    <div className="frame-layout">
      <header className="frame-header">
        <button className="frame-back" onClick={onBack} title="Back to dashboard">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="m15 18-6-6 6-6" /></svg>
          Dashboard
        </button>
        <div className="frame-title">
          <img src={`/icons/${service.icon}`} alt="" className="frame-title__icon"
            onError={e => { (e.currentTarget as HTMLImageElement).style.display = 'none' }} />
          <span>{service.name}</span>
          <span className={`card__dot card__dot--${service.isUp ? 'up' : 'down'}`} />
        </div>
        <a className="frame-newtab" href={src} target="_blank" rel="noopener noreferrer" title="Open in new tab">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M15 3h6v6" /><path d="M10 14 21 3" /><path d="M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6" /></svg>
        </a>
      </header>

      <div className="frame-stage">
        {loading && (
          <div className="frame-loading">
            <div className="splash__spinner" />
            <p className="splash__text">Loading {service.name}…</p>
          </div>
        )}
        <iframe
          ref={iframeRef}
          src={src}
          title={service.name}
          className="frame-iframe"
          onLoad={() => setLoading(false)}
          allow="clipboard-read; clipboard-write; fullscreen; autoplay; encrypted-media"
        />
      </div>
    </div>
  )
}
