import { Box, Card, Text, UnstyledButton } from '@mantine/core'
import type { FlightCabin } from '../model/flight.types'
import styles from '../styles/CabinSelector.module.scss'

interface CabinSelectorProps {
  cabins: FlightCabin[]
  selectedCabinId: number | null
  onSelect: (cabinId: number) => void
}

export function CabinSelector({ cabins, selectedCabinId, onSelect }: CabinSelectorProps) {
  return (
    <Box className={styles.selector}>
      {cabins.map((cabin) => {
        const isSelected = selectedCabinId === cabin.classId
        const lowestPrice = Math.min(...cabin.fares.map((fare) => fare.farePriceUsd))

        return (
          <UnstyledButton key={cabin.classId} onClick={() => onSelect(cabin.classId)}>
            <Card
              className={`${styles.card} ${isSelected ? styles.selected : ''}`}
              withBorder
              p="sm"
            >
              <Text className={styles.name} size="xs" c={isSelected ? undefined : 'dimmed'} fw={500}>
                {cabin.className}
              </Text>
              <Text className={`${styles.price} ${isSelected ? styles.selectedPrice : ''}`} size="md" fw={700}>
                ${lowestPrice.toLocaleString()}
              </Text>
            </Card>
          </UnstyledButton>
        )
      })}
    </Box>
  )
}