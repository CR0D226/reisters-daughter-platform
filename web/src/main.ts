import { createApp } from 'vue'
import { createPinia } from 'pinia'

import './assets/main.css'

import App from './App.vue'
import router from './router'
import keycloak from './auth/keycloak'

async function bootstrap() {
  try {
    const authenticated = await keycloak.init({
      onLoad: 'check-sso',
      pkceMethod: 'S256',
      checkLoginIframe: false,
    })

    console.log(
      authenticated
        ? `Authenticated as ${keycloak.tokenParsed?.preferred_username}`
        : 'Not authenticated',
    )
  } catch (error) {
    console.error('Failed to initialize Keycloak:', error)
  }

  const app = createApp(App)

  app.use(createPinia())
  app.use(router)

  app.provide('keycloak', keycloak)

  await router.isReady()

  app.mount('#app')
}

bootstrap()