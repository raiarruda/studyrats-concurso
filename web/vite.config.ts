import { fileURLToPath, URL } from 'node:url'
import vue from '@vitejs/plugin-vue'
import { defineConfig } from 'vite'
import { VitePWA } from 'vite-plugin-pwa'

// O navegador só fala com este servidor (porta 5173). Ele repassa /api e /uploads para a API,
// que continua escutando só em localhost. `xfwd` entrega o IP real de cada colega à API,
// senão o limite de tentativas de login trataria todo mundo como um único cliente (127.0.0.1).
const apiProxy = {
  '/api': { target: 'http://localhost:5080', xfwd: true },
  '/uploads': { target: 'http://localhost:5080', xfwd: true },
}

export default defineConfig({
  plugins: [
    vue(),
    VitePWA({
      registerType: 'autoUpdate',
      includeAssets: ['favicon.svg', 'apple-touch-icon.png'],
      manifest: {
        name: 'StudyRats',
        short_name: 'StudyRats',
        description: 'Desafios de estudo para concurseiros, entre amigos.',
        lang: 'pt-BR',
        theme_color: '#4f46e5',
        background_color: '#f6f6fb',
        display: 'standalone',
        start_url: '/',
        icons: [
          { src: 'icon-192.png', sizes: '192x192', type: 'image/png' },
          { src: 'icon-512.png', sizes: '512x512', type: 'image/png' },
          { src: 'icon-maskable-512.png', sizes: '512x512', type: 'image/png', purpose: 'maskable' },
        ],
      },
      workbox: {
        // Só o "casco" do app fica em cache. API e fotos sempre vão à rede.
        navigateFallbackDenylist: [/^\/api/, /^\/uploads/],
      },
    }),
  ],
  resolve: {
    alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) },
  },
  // host: true = escuta em todas as interfaces, para os colegas da rede acessarem.
  server: { host: true, proxy: apiProxy },
  preview: { host: true, proxy: apiProxy },
})
