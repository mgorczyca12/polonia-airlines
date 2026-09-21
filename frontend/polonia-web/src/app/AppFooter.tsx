import { Box, Text } from '@mantine/core'
import styles from './AppFooter.module.scss'

export function AppFooter() {
  return (
    <Box className={styles.footer}>
      <Text size="sm" c="dimmed">Polonia Airlines</Text>
      <Text size="sm" c="dimmed">Plan, select, and book your trip.</Text>
    </Box>
  )
}