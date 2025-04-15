import http.server
import socketserver
import ssl
import os

class GzipHandler(http.server.SimpleHTTPRequestHandler):
    def end_headers(self):
        self.send_header('Access-Control-Allow-Origin', '*')
        if self.path.endswith('.gz'):
            self.send_header('Content-Encoding', 'gzip')
        super().end_headers()

PORT = 8000
httpd = socketserver.TCPServer(("", PORT), GzipHandler)

# Use absolute path to 'Server/cert.pem' and 'Server/key.pem'
cert_path = os.path.join("Server", "cert.pem")
key_path = os.path.join("Server", "key.pem")

context = ssl.SSLContext(ssl.PROTOCOL_TLS_SERVER)
context.load_cert_chain(certfile=cert_path, keyfile=key_path)

httpd.socket = context.wrap_socket(httpd.socket, server_side=True)

print(f"HTTPS server running at https://localhost:{PORT}")
httpd.serve_forever()