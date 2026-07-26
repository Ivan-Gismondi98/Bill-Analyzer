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
    rollupOptions: {
      output: {
        // Nomi di file DETERMINISTICI (senza hash del contenuto).
        //
        // MSBuild fotografa l'elenco dei MauiAsset quando carica il progetto, prima che
        // questo build venga eseguito. Con i nomi hashati di default, ogni modifica al
        // frontend generava file nuovi e cancellava i vecchi: MSBuild tentava di copiare
        // i file della fotografia precedente, ormai inesistenti (errore XA2001).
        // Con nomi fissi la lista non cambia mai e il build resta ripetibile.
        //
        // L'hash serve a invalidare le cache HTTP: qui gli asset sono impacchettati
        // nell'app e serviti localmente, quindi non è necessario.
        entryFileNames: 'assets/[name].js',
        chunkFileNames: 'assets/[name].js',
        assetFileNames: 'assets/[name].[ext]',
      },
    },
  },
  server: {
    port: 5173,
    proxy: {
      // In sviluppo il browser proxya le chiamate API verso il backend .NET.
      '/api': 'http://localhost:5080',
    },
  },
});
