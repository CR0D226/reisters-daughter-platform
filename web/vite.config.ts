import { fileURLToPath, URL } from 'node:url'
import { readFileSync } from 'node:fs'

import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import vueDevTools from 'vite-plugin-vue-devtools'

export default defineConfig({
  plugins: [
    vue(),
    vueDevTools(),
  ],

  server: {
    host: '0.0.0.0',
    port: 5173,
    strictPort: true,

    https: {
      key: readFileSync(
        fileURLToPath(new URL('./certs/dev-key.pem', import.meta.url))
      ),
      cert: readFileSync(
        fileURLToPath(new URL('./certs/dev.pem', import.meta.url))
      ),
    },

    proxy: {
      '/api': {
        target: 'http://127.0.0.1:5128',
        changeOrigin: true,
      },
    },
  },

  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
})