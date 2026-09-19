import { Box, Container, Text, Title } from '@mantine/core'
import { FlightSearch } from '#/features/flight-search/components/FlightSearch'
import { FlightResults } from '#/features/flight-results/components/FlightResults'
import styles from './styles/HomePage.module.scss'

interface HomePageProps {
  from?: string
  to?: string
}

export function HomePage({ from, to }: HomePageProps) {
  const hasSearched = Boolean(from || to)

  return (
    <Container size="lg" py="xl">
      <Box className={styles.page}>
        <Box className={styles.sectionHeading}>
          <Text c="dimmed" size="sm">Flight search</Text>
          <Title order={1}>Plan your trip</Title>
        </Box>
        <FlightSearch />
        <Box className={styles.sectionHeading}>
          <Text c="dimmed" size="sm">{hasSearched ? 'Available flights' : 'Featured routes'}</Text>
          <Title order={2} size="h3">{hasSearched ? 'Search results' : 'Flights from our network'}</Title>
        </Box>
        <FlightResults from={from} to={to} />
      </Box>
    </Container>
  )
}