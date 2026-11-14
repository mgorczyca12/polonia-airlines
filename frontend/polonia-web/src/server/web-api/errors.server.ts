/** An expected failure with a stable code, returned to the browser as safe JSON. */
export class HttpError extends Error {
  constructor(
    public readonly status: number,
    public readonly code: string,
    message: string,
  ) {
    super(message)
  }
}

export function jsonResponse(
  data: unknown,
  status: number,
  headers: Headers,
): Response {
  return new Response(JSON.stringify(data), { status, headers })
}

/**
 * Maps a failure to ProblemDetails-like JSON. Unexpected errors are logged with a
 * trace identifier and never expose their message to the browser.
 */
export function errorResponse(error: unknown, headers: Headers): Response {
  if (error instanceof HttpError) {
    return jsonResponse(
      { status: error.status, code: error.code, detail: error.message },
      error.status,
      headers,
    )
  }

  const traceId = crypto.randomUUID()
  console.error('Web API failed', { traceId, error })
  return jsonResponse(
    {
      status: 500,
      code: 'web_api_error',
      detail: `An unexpected error occurred. Reference: ${traceId}`,
    },
    500,
    headers,
  )
}
