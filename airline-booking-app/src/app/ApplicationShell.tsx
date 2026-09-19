import type { ReactNode } from 'react'
import { AppShell } from '@mantine/core'
import { AppFooter } from './AppFooter'
import { AppHeader } from './AppHeader'

interface ApplicationShellProps {
  children: ReactNode
}

export function ApplicationShell({ children }: ApplicationShellProps) {
  return (
    <AppShell header={{ height: 60 }} footer={{ height: 56 }} padding="md">
      <AppShell.Header p="sm">
        <AppHeader />
      </AppShell.Header>
      <AppShell.Main>{children}</AppShell.Main>
      <AppShell.Footer p="sm">
        <AppFooter />
      </AppShell.Footer>
    </AppShell>
  )
}