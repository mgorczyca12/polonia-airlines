import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import {
  isTerminal,
  reservationKeys,
  reservationQuery,
} from '#/entities/reservation'
import { errorMessage } from '#/shared/api'
import type { ReservationResponse } from '#/shared/api'

const MAX_STATUS_POLLS = 40
const FIRST_POLL_DELAY_MS = 1500
const POLL_BACKOFF_MS = 500
const MAX_POLL_DELAY_MS = 5000

/**
 * Polls a submitted reservation with bounded, increasing delays until its
 * workflow reaches a terminal state. A timeout or network failure never implies
 * cancellation; the caller can retry status explicitly.
 */
export function useReservationStatus(
  reservationId: number | null,
  recovered?: { reservation: ReservationResponse; updatedAt: number },
) {
  const queryClient = useQueryClient()
  const [pollWindowStart, setPollWindowStart] = useState(0)
  const seed =
    recovered?.reservation.id === reservationId ? recovered : undefined
  const query = useQuery({
    ...reservationQuery(reservationId ?? 0),
    enabled: reservationId !== null,
    retry: false,
    initialData: seed?.reservation,
    initialDataUpdatedAt: seed?.updatedAt,
    // Seeded data is fresh; the first poll waits for the interval below.
    staleTime: FIRST_POLL_DELAY_MS,
    refetchInterval: ({ state }) => {
      const attempts = state.dataUpdateCount - pollWindowStart

      if (
        !state.data ||
        isTerminal(state.data) ||
        state.status === 'error' ||
        attempts >= MAX_STATUS_POLLS
      ) {
        return false
      }

      return Math.min(
        FIRST_POLL_DELAY_MS + attempts * POLL_BACKOFF_MS,
        MAX_POLL_DELAY_MS,
      )
    },
  })
  const reservation = query.data
  const updateCount =
    reservationId === null
      ? 0
      : (queryClient.getQueryState(reservationKeys.detail(reservationId))
          ?.dataUpdateCount ?? 0)
  const timedOut =
    reservation !== undefined &&
    !isTerminal(reservation) &&
    updateCount - pollWindowStart >= MAX_STATUS_POLLS

  let statusProblem: string | null = null

  if (query.isError) {
    statusProblem = errorMessage(query.error)
  } else if (timedOut) {
    statusProblem =
      'Confirmation is taking longer than expected. Your reservation is still pending; retry status, not a new booking.'
  }

  const retryStatus = () => {
    setPollWindowStart(updateCount)
    void query.refetch()
  }

  return {
    reservation,
    statusProblem,
    isRefreshing: query.isFetching,
    retryStatus,
  }
}
