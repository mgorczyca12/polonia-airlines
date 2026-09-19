import { Box, Card, Divider, Text, UnstyledButton } from '@mantine/core'
import type { FlightCabin } from '../model/flight.types'
import styles from '../styles/FareOptions.module.scss'

interface FareOptionsProps {
  cabin: FlightCabin
  selectedFareId: number | null
  onSelect: (fareId: number) => void
}

export function FareOptions({ cabin, selectedFareId, onSelect }: FareOptionsProps) {
  return (
    <Box className={styles.container}>
      <Divider label={`Select a ${cabin.className} fare`} labelPosition="center" />
      <Box className={styles.options}>
        {cabin.fares.map((fare) => {
          const isSelected = selectedFareId === fare.fareId

          return (
            <UnstyledButton key={fare.fareId} onClick={() => onSelect(fare.fareId)}>
              <Card className={`${styles.card} ${isSelected ? styles.selected : ''}`} withBorder>
                <Box className={styles.fareContent}>
                  <div>
                    <Text className={isSelected ? styles.selectedText : styles.defaultText} fw={700} size="md">
                      {fare.fareName}
                    </Text>
                    {fare.perks.slice(0, 2).map((perk) => (
                      <Text className={isSelected ? styles.perkSelected : ''} key={perk} size="xs" c={isSelected ? undefined : 'dimmed'} mt={8}>
                        • {perk}
                      </Text>
                    ))}
                  </div>
                  <Text className={isSelected ? styles.selectedText : styles.defaultText} fw={700} size="lg" ta="right">
                    ${fare.farePriceUsd.toLocaleString()}
                  </Text>
                </Box>
              </Card>
            </UnstyledButton>
          )
        })}
      </Box>
    </Box>
  )
}
