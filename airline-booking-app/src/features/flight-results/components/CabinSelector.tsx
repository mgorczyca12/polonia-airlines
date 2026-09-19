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
              withBorder
              p="sm"
              className={`${styles.card} ${isSelected ? styles.selected : ''}`}
            >
              <Text className={`${styles.name} ${isSelected ? styles.selectedText : ''}`} size="xs" c={isSelected ? undefined : 'dimmed'} fw={500}>
                {cabin.className}
              </Text>
              <Text className={`${styles.price} ${isSelected ? styles.selectedPrice : styles.defaultPrice}`} size="md" fw={700}>
                ${lowestPrice.toLocaleString()}
              </Text>
            </Card>
          </UnstyledButton>
        )
      })}
    </Box>
  )
}