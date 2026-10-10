import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// The API runs on http://localhost:5042 (dotnet run). Proxying keeps the SPA same-origin in development, so no CORS setup is needed.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': 'http://localhost:5042',
      '/health': 'http://localhost:5042',
    },
  },
})
