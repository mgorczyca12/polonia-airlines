import { Link } from '@tanstack/react-router'
import { ActionIcon, Box, Text, useMantineColorScheme } from '@mantine/core'
import { IconMoon, IconPlane, IconSun } from '@tabler/icons-react'
import styles from './AppHeader.module.scss'

export function AppHeader() {
  const { colorScheme, setColorScheme } = useMantineColorScheme()
  const isDark = colorScheme === 'dark'

  return (
    <Box className={styles.header}>
      <Box className={styles.brand}>
        <IconPlane size={22} />
        <Text fw={700}>Polonia Airlines</Text>
      </Box>
      <Box className={styles.navigation}>
        <Link className={styles.link} to="/">Search flights</Link>
        <Link className={styles.link} to="/booking">Booking</Link>
        <ActionIcon
          className={styles.themeToggle}
          variant="subtle"
          color={isDark ? 'yellow' : 'red'}
          aria-label={isDark ? 'Switch to light theme' : 'Switch to dark theme'}
          title={isDark ? 'Switch to light theme' : 'Switch to dark theme'}
          onClick={() => setColorScheme(isDark ? 'light' : 'dark')}
        >
          {isDark ? <IconSun size={18} /> : <IconMoon size={18} />}
        </ActionIcon>
      </Box>
    </Box>
  )
}