import { createFileRoute } from '@tanstack/react-router'
import { Box, Container, Text, Title } from '@mantine/core'
import { FlightSearch } from '#/features/flight-search/components/FlightSearch'

export const Route = createFileRoute('/')({
  component: HomeRoute,
})

function HomeRoute() {
  return (
    <Container size="lg" py="xl">
      <Box>
        <Box>
          <Text c="dimmed" size="sm">Flight search</Text>
          <Title order={1}>Plan your trip</Title>
        </Box>
        <FlightSearch />
      </Box>
    </Container>
  )
}
