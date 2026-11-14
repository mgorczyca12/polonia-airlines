export interface WebApiConfig {
  password: string
  origin: string
  secure: boolean
}

/** Reads and validates server-only configuration on every request. */
export function readConfig(): WebApiConfig {
  const password = process.env.WEB_SESSION_PASSWORD
  const origin = process.env.WEB_ORIGIN

  if (!password || password.length < 32 || !origin) {
    throw new Error(
      'WEB_SESSION_PASSWORD (32+ characters) and WEB_ORIGIN are required.',
    )
  }

  const url = new URL(origin)

  if (url.origin !== origin || !['http:', 'https:'].includes(url.protocol)) {
    throw new Error('WEB_ORIGIN must be an HTTP(S) origin without a path.')
  }

  if (
    url.protocol === 'http:' &&
    !['localhost', '127.0.0.1'].includes(url.hostname)
  ) {
    throw new Error('Non-local web origins must use HTTPS.')
  }

  return { password, origin, secure: url.protocol === 'https:' }
}
