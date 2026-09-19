import { Box, Button, Card, Collapse } from '@mantine/core'
import { useDisclosure } from '@mantine/hooks'
import { useNavigate } from '@tanstack/react-router'
import { useState } from 'react'
import type { Flight } from '../model/flight.types'
import { CabinSelector } from './CabinSelector'
import { FareOptions } from './FareOptions'
import { FlightDetails } from './FlightDetails'
import { FlightTimeline } from './FlightTimeline'
import styles from '../styles/FlightResultCard.module.scss'

interface FlightResultCardProps {
  flight: Flight
}

export function FlightResultCard({ flight }: FlightResultCardProps) {
  const navigate = useNavigate()
  const [detailsOpened, { toggle: toggleDetails }] = useDisclosure(false)
  const [selectedCabinId, setSelectedCabinId] = useState<number | null>(null)
  const [selectedFareId, setSelectedFareId] = useState<number | null>(null)
  const selectedCabin = flight.brandedFares.find((cabin) => cabin.classId === selectedCabinId)

  const handleCabinSelect = (cabinId: number) => {
    setSelectedCabinId((currentId) => currentId === cabinId ? null : cabinId)
    setSelectedFareId(null)
  }

  return (
    <Card className={styles.card} withBorder p="xl">
      <Box className={styles.content}>
        <Box className={styles.header}>
          <FlightTimeline
            flight={flight}
            detailsOpened={detailsOpened}
            onToggleDetails={toggleDetails}
          />
          <CabinSelector
            cabins={flight.brandedFares}
            selectedCabinId={selectedCabinId}
            onSelect={handleCabinSelect}
          />
        </Box>

        <FlightDetails flight={flight} opened={detailsOpened} />

        <Collapse expanded={selectedCabin !== undefined}>
          {selectedCabin && (
            <Box className={styles.content}>
              <FareOptions
                cabin={selectedCabin}
                selectedFareId={selectedFareId}
                onSelect={setSelectedFareId}
              />
              <Button
                className={styles.continueButton}
                disabled={selectedFareId === null}
                onClick={() => {
                  if (selectedFareId !== null) {
                    void navigate({ to: '/booking', search: { flight: flight.flightNumber, fare: selectedFareId } })
                  }
                }}
              >
                Continue to booking
              </Button>
            </Box>
          )}
        </Collapse>
      </Box>
    </Card>
  )
}