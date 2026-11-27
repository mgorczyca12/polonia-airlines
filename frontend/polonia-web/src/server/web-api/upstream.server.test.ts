import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

describe('upstream resilience', () => {
  const fetchMock = vi.fn<typeof fetch>()

  beforeEach(() => {
    vi.resetModules()
    vi.useFakeTimers()
    vi.stubEnv('FLIGHT_API_URL', 'http://flight.test')
    vi.stubEnv('BOOKING_API_URL', 'http://booking.test')
    vi.stubGlobal('fetch', fetchMock)
    vi.spyOn(console, 'error').mockImplementation(() => {})
    fetchMock.mockReset()
  })

  afterEach(() => {
    vi.useRealTimers()
    vi.unstubAllGlobals()
    vi.unstubAllEnvs()
    vi.restoreAllMocks()
  })

  it('retries transient GET failures and passes the successful result through', async () => {
    const { callUpstream } = await import('./upstream.server')
    fetchMock
      .mockRejectedValueOnce(new TypeError('network failure'))
      .mockResolvedValueOnce(Response.json({}, { status: 503 }))
      .mockResolvedValueOnce(Response.json({ flights: [] }))
    const result = callUpstream({
      service: 'flight',
      path: '/flights',
      request: new Request('http://localhost/api/web/flights'),
    })
    await vi.runAllTimersAsync()
    await expect(result).resolves.toEqual({
      status: 200,
      data: { flights: [] },
    })
    expect(fetchMock).toHaveBeenCalledTimes(3)
  })

  it('does not retry client errors or writes', async () => {
    const { callUpstream } = await import('./upstream.server')
    fetchMock.mockResolvedValueOnce(Response.json({}, { status: 400 }))
    await expect(
      callUpstream({
        service: 'flight',
        path: '/flights',
        request: new Request('http://localhost/api/web/flights'),
      }),
    ).resolves.toMatchObject({ status: 400 })
    expect(fetchMock).toHaveBeenCalledTimes(1)

    fetchMock.mockResolvedValueOnce(Response.json({}, { status: 503 }))
    await expect(
      callUpstream({
        service: 'booking',
        path: '/reservations',
        request: new Request('http://localhost/api/web/reservations', {
          method: 'POST',
        }),
      }),
    ).rejects.toMatchObject({ status: 502, code: 'backend_unavailable' })
    expect(fetchMock).toHaveBeenCalledTimes(2)
  })

  it('returns a safe failure after exhausting GET retries', async () => {
    const { callUpstream } = await import('./upstream.server')
    fetchMock.mockImplementation(async () =>
      Response.json({ detail: 'private' }, { status: 503 }),
    )
    const result = callUpstream({
      service: 'flight',
      path: '/flights',
      request: new Request('http://localhost/api/web/flights'),
    })
    const rejection = expect(result).rejects.toMatchObject({
      status: 502,
      code: 'backend_unavailable',
    })
    await vi.runAllTimersAsync()
    await rejection
    expect(fetchMock).toHaveBeenCalledTimes(3)
  })

  it('opens each backend circuit independently and recovers after a successful probe', async () => {
    const { callUpstream } = await import('./upstream.server')
    const flight = () =>
      callUpstream({
        service: 'flight',
        path: '/flights',
        request: new Request('http://localhost/api/web/flights', {
          method: 'POST',
        }),
      })
    fetchMock.mockImplementation(async () => Response.json({}, { status: 503 }))
    for (let attempt = 0; attempt < 5; attempt++) {
      await expect(flight()).rejects.toMatchObject({ status: 502 })
    }
    await expect(flight()).rejects.toMatchObject({ status: 502 })
    expect(fetchMock).toHaveBeenCalledTimes(5)

    fetchMock.mockImplementation(async () => Response.json({ ok: true }))
    await expect(
      callUpstream({
        service: 'booking',
        path: '/fares',
        request: new Request('http://localhost/api/web/fares'),
      }),
    ).resolves.toMatchObject({ status: 200 })

    vi.advanceTimersByTime(30_001)
    await expect(flight()).resolves.toMatchObject({ status: 200 })
    await expect(flight()).resolves.toMatchObject({ status: 200 })
    expect(fetchMock).toHaveBeenCalledTimes(8)
  })

  it('stops promptly on cancellation during backoff without another request', async () => {
    const { callUpstream } = await import('./upstream.server')
    const controller = new AbortController()
    fetchMock.mockRejectedValue(new TypeError('network failure'))
    const result = callUpstream({
      service: 'flight',
      path: '/flights',
      request: new Request('http://localhost/api/web/flights', {
        signal: controller.signal,
      }),
    })
    const rejection = expect(result).rejects.toMatchObject({
      name: 'AbortError',
    })
    await vi.advanceTimersByTimeAsync(0)
    controller.abort()
    await rejection
    await vi.runAllTimersAsync()
    expect(fetchMock).toHaveBeenCalledTimes(1)
  })

  it('limits the entire upstream operation to 15 seconds', async () => {
    const { callUpstream } = await import('./upstream.server')
    vi.spyOn(AbortSignal, 'timeout').mockImplementation((milliseconds) => {
      const controller = new AbortController()
      setTimeout(
        () => controller.abort(new DOMException('Timed out', 'TimeoutError')),
        milliseconds,
      )
      return controller.signal
    })
    fetchMock.mockImplementation(
      (_url, init) =>
        new Promise((_resolve, reject) => {
          init?.signal?.addEventListener('abort', () =>
            reject(init.signal?.reason),
          )
        }),
    )
    const result = callUpstream({
      service: 'flight',
      path: '/flights',
      request: new Request('http://localhost/api/web/flights'),
    })
    const rejection = expect(result).rejects.toMatchObject({
      status: 502,
      code: 'backend_unavailable',
    })
    await vi.advanceTimersByTimeAsync(14_999)
    expect(fetchMock).toHaveBeenCalledTimes(1)
    await vi.advanceTimersByTimeAsync(1)
    await rejection
    expect(fetchMock).toHaveBeenCalledTimes(1)
  })

  it('does not close an open circuit when its recovery probe is cancelled', async () => {
    const { callUpstream } = await import('./upstream.server')
    const flight = (signal?: AbortSignal) =>
      callUpstream({
        service: 'flight',
        path: '/flights',
        request: new Request('http://localhost/api/web/flights', {
          method: 'POST',
          signal,
        }),
      })
    fetchMock.mockImplementation(async () => Response.json({}, { status: 503 }))
    for (let attempt = 0; attempt < 5; attempt++) {
      await expect(flight()).rejects.toMatchObject({ status: 502 })
    }

    vi.advanceTimersByTime(30_001)
    const controller = new AbortController()
    fetchMock.mockImplementation(
      (_url, init) =>
        new Promise((_resolve, reject) => {
          init?.signal?.addEventListener('abort', () =>
            reject(init.signal?.reason),
          )
        }),
    )
    const probe = flight(controller.signal)
    controller.abort()
    await expect(probe).rejects.toMatchObject({ name: 'AbortError' })
    await expect(flight()).rejects.toMatchObject({ status: 502 })
    expect(fetchMock).toHaveBeenCalledTimes(6)
  })
})
