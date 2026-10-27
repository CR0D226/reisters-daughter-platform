import keycloak from '@/auth/keycloak'

const API_BASE =
  import.meta.env.VITE_API_BASE_URL ??
  'http://localhost:5128'

export async function apiFetch(
  path: string,
  options: RequestInit = {},
): Promise<Response> {
  if (!keycloak.authenticated) {
    throw new Error('Authentication required')
  }

  await keycloak.updateToken(30)

  if (!keycloak.token) {
    throw new Error('No access token available')
  }

  const headers = new Headers(options.headers)

  headers.set(
    'Authorization',
    `Bearer ${keycloak.token}`,
  )

  return fetch(`${API_BASE}${path}`, {
    ...options,
    headers,
  })
}