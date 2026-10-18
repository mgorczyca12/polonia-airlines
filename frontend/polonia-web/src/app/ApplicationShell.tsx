import type { ReactNode } from 'react'
import { AppFooter } from './AppFooter'
import { AppHeader } from './AppHeader'
import styles from './ApplicationShell.module.scss'

interface ApplicationShellProps {
  children: ReactNode
}

export function ApplicationShell({ children }: ApplicationShellProps) {
  return (
    <div className={styles.shell}>
      <AppHeader />
      <main className={styles.main}>{children}</main>
      <AppFooter />
    </div>
  )
}
