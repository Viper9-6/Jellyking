#!/usr/bin/env python3
"""Multi-port mock media service for Jellyking end-to-end tests.

Listens on the ports Jellyking expects for sonarr (8989) and
qbittorrent with a custom override (18080). Responds 200 OK on each
service's health path and serves a tiny HTML page at the base path.
"""
import threading
from http.server import BaseHTTPRequestHandler, HTTPServer

SERVICES = {
    8989: {
        'id': 'sonarr',
        'base_path': '/sonarr',
        'health_path': '/sonarr/api/v3/system/status',
        'title': 'Mock Sonarr',
    },
    18080: {
        'id': 'qbittorrent',
        'base_path': '/qbit',
        'health_path': '/qbit/api/v2/app/version',
        'title': 'Mock qBittorrent (custom port 18080)',
    },
}


def make_handler(cfg):
    """Create a request handler class bound to the given service config.

    We subclass BaseHTTPRequestHandler so the config is available before
    __init__ runs (BaseHTTPRequestHandler.__init__ calls handle() which
    calls do_GET()).
    """
    class BoundHandler(BaseHTTPRequestHandler):
        def do_GET(self):
            path = self.path

            if path == cfg['health_path']:
                self.send_response(200)
                self.send_header('Content-Type', 'application/json')
                self.end_headers()
                self.wfile.write(b'{"status": "ok"}')
                return

            if path.startswith(cfg['base_path'] + '/'):
                body = f"""<!doctype html>
<html>
<head><title>{cfg['title']}</title></head>
<body>
  <h1>{cfg['title']}</h1>
  <p>Proxied through Jellyking at <code>{cfg['base_path']}/</code></p>
  <p>Request path: <code>{path}</code></p>
</body>
</html>""".encode('utf-8')
                self.send_response(200)
                self.send_header('Content-Type', 'text/html; charset=utf-8')
                self.send_header('Content-Length', str(len(body)))
                self.end_headers()
                self.wfile.write(body)
                return

            self.send_response(404)
            self.end_headers()

        def log_message(self, fmt, *args):
            print(f"[mock:{cfg['id']}] {args[0]} {args[1]}")

    return BoundHandler


def make_server(port, cfg):
    return HTTPServer(('127.0.0.1', port), make_handler(cfg))


def main():
    threads = []
    for port, cfg in SERVICES.items():
        server = make_server(port, cfg)
        t = threading.Thread(target=server.serve_forever, daemon=True)
        t.start()
        threads.append(t)
        print(f"Mock {cfg['id']} listening on http://127.0.0.1:{port}")

    print("Mock services are running. Press Ctrl+C to stop.")
    try:
        threading.Event().wait()
    except KeyboardInterrupt:
        print("\nStopping mock services...")


if __name__ == '__main__':
    main()
