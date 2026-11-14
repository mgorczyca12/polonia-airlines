import { isRecord } from './guards'

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    public readonly code: string,
    message: string,
    public readonly errors: Record<string, string[]> = {},
  ) {
    super(message)
  }
}

function readValidationErrors(data: unknown): Record<string, string[]> {
  const errors: Record<string, string[]> = {}

  if (!isRecord(data) || !isRecord(data.errors)) {
    return errors
  }

  for (const [key, messages] of Object.entries(data.errors)) {
    if (
      Array.isArray(messages) &&
      messages.every((message) => typeof message === 'string')
    ) {
      errors[key] = messages
    }
  }

  return errors
}

/** Calls the same-origin web API layer and throws an `ApiError` for non-2xx responses. */
export async function request(
  path: string,
  signal?: AbortSignal,
  body?: unknown,
): Promise<unknown> {
  const hasBody = body !== undefined
  const response = await fetch(`/api/web/${path}`, {
    method: hasBody ? 'POST' : 'GET',
    credentials: 'same-origin',
    cache: 'no-store',
    signal,
    headers: hasBody ? { 'Content-Type': 'application/json' } : undefined,
    body: hasBody ? JSON.stringify(body) : undefined,
  })
  const data: unknown = await response.json()

  if (!response.ok) {
    throw new ApiError(
      response.status,
      isRecord(data) && typeof data.code === 'string'
        ? data.code
        : 'request_failed',
      isRecord(data) && typeof data.detail === 'string'
        ? data.detail
        : 'The request failed. Please try again.',
      readValidationErrors(data),
    )
  }

  return data
}

export function isNotFound(error: unknown): boolean {
  return (
    error instanceof ApiError &&
    error.status === 404 &&
    error.code === 'not_found'
  )
}

export function errorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    const fields = Object.values(error.errors).flat().join(' ')
    return fields ? `${error.message} ${fields}` : error.message
  }

  return error instanceof Error
    ? error.message
    : 'An unexpected error occurred.'
}
