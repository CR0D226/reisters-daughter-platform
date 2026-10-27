import Keycloak from 'keycloak-js'

const keycloak = new Keycloak({
  url: import.meta.env.VITE_KEYCLOAK_URL
    ?? 'https://192.168.0.176:8443',
  realm: 'reisters-daughter',
  clientId: 'reisters-web',
})

export default keycloak