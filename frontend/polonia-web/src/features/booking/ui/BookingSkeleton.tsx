import { Skeleton, Stack } from '@mantine/core'
import { LoadingRegion } from '#/shared/ui/LoadingRegion'

/** First-load placeholder shaped like a traveller panel and contact section. */
export function BookingSkeleton() {
  return (
    <LoadingRegion label="Loading booking">
      <Stack gap="md">
        <Skeleton height={180} radius="md" />
        <Skeleton height={120} radius="md" />
      </Stack>
    </LoadingRegion>
  )
}
