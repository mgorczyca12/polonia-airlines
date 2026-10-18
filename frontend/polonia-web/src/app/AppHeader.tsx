import { Link, useRouterState } from '@tanstack/react-router'
import {
  ActionIcon,
  VisuallyHidden,
  useComputedColorScheme,
  useMantineColorScheme,
} from '@mantine/core'
import { Globe2, Moon, Plane, Sun } from 'lucide-react'
import styles from './AppHeader.module.scss'

const BOOKING_FLOW_PATHS = ['/', '/flightlist', '/booking']

export function AppHeader() {
  const { setColorScheme } = useMantineColorScheme()
  const computedColorScheme = useComputedColorScheme('light', {
    getInitialValueInEffect: true,
  })
  const pathname = useRouterState({ select: (state) => state.location.pathname })
  const isDark = computedColorScheme === 'dark'
  const isBookingFlow = BOOKING_FLOW_PATHS.includes(pathname)
  const themeLabel = isDark ? 'Switch to light theme' : 'Switch to dark theme'

  return (
    <header className={styles.header}>
      <div className={styles.inner}>
        <Link className={styles.brand} to="/" aria-label="Polonia Airlines home">
          <span className={styles.emblem} aria-hidden="true">
            <Plane size={24} strokeWidth={1.75} />
          </span>
          <span className={styles.wordmark}>
            <span className={styles.wordmarkName}>POLONIA</span>
            <span className={styles.wordmarkDescriptor}>AIRLINES</span>
          </span>
        </Link>

        <nav className={styles.navigation} aria-label="Primary">
          <Link
            className={`${styles.navLink} ${isBookingFlow ? styles.navLinkActive : ''}`}
            to="/"
            aria-current={isBookingFlow ? 'page' : undefined}
          >
            Book a journey
          </Link>
        </nav>

        <div className={styles.account}>
          <span className={styles.locale}>
            <Globe2 size={17} strokeWidth={1.75} aria-hidden="true" />
            <span aria-hidden="true">EN · USD</span>
            <VisuallyHidden>Language: English. Prices in US dollars.</VisuallyHidden>
          </span>
          <ActionIcon
            variant="subtle"
            color="gray"
            size="lg"
            aria-label={themeLabel}
            title={themeLabel}
            onClick={() => setColorScheme(isDark ? 'light' : 'dark')}
          >
            {isDark ? <Sun size={18} /> : <Moon size={18} />}
          </ActionIcon>
        </div>
      </div>
    </header>
  )
}
