import { Box, Container, Title, Text } from "@mantine/core";
import { FlightResults } from "../features/flight-results/components/FlightResults";

interface FlightListingPageProps {
  from?: string
  to?: string
}

export function FlightListingPage({ from, to }: FlightListingPageProps) {
  return (
    <Container>
      <Box>
        <Text c="dimmed" size="sm">Available flights</Text>
        <Title order={2} size="h3">Search results</Title>
      </Box>
      <FlightResults from={from} to={to} />
    </Container>
  )
}