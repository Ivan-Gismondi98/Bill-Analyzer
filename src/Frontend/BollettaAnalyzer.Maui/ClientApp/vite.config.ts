import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// Build output finisce in Resources/Raw/wwwroot così il MAUI HybridWebView
// può servire l'app come contenuto statico impacchettato nell'app.
export default defineConfig({
  plugins: [react()],
  base: './',
  build: {
    outDir: '../Resources/Raw/wwwroot',
    emptyOutDir: true,
  },
  server: {
    port: 5173,
    proxy: {
      // In sviluppo il browser proxya le chiamate API verso il backend .NET.
      '/api': 'http://localhost:5080',
    },
  },
});
