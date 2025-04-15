import http.server
import socketserver
import os

class GzipHandler(http.server.SimpleHTTPRequestHandler):
    def end_headers(self):
        self.send_header('Access-Control-Allow-Origin', '*')
        if self.path.endswith('.gz'):
            self.send_header('Content-Encoding', 'gzip')
        http.server.SimpleHTTPRequestHandler.end_headers(self)

PORT = 8000
with socketserver.TCPServer(("", PORT), GzipHandler) as httpd:
    print("Server running at http://localhost:" + str(PORT))
    httpd.serve_forever()