import { createFileRoute } from '@tanstack/react-router'
import { handleWebApi } from '#/shared/api/web-api.server'

export const Route = createFileRoute('/api/web/$')({
  server: {
    handlers: {
      GET: ({ request }) => handleWebApi(request),
      POST: ({ request }) => handleWebApi(request),
    },
  },
})
