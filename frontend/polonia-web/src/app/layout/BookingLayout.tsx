import type { ReactNode } from 'react'
import styles from './BookingLayout.module.scss'

interface BookingLayoutProps {
  eyebrow: string
  title: string
  description?: string
  sidebar?: ReactNode
  children: ReactNode
}

export function BookingNextStep({
  note,
  children,
}: {
  note: ReactNode
  children: ReactNode
}) {
  return (
    <div className={styles.nextStep}>
      <p className={styles.nextStepNote}>{note}</p>
      {children}
    </div>
  )
}

export function BookingLayout({
  eyebrow,
  title,
  description,
  sidebar,
  children,
}: BookingLayoutProps) {
  return (
    <div className={styles.page}>
      <header className={styles.heading}>
        <p className={styles.eyebrow}>{eyebrow}</p>
        <h1 className={styles.title}>{title}</h1>
        {description && <p className={styles.description}>{description}</p>}
      </header>
      <div className={sidebar ? styles.withSidebar : undefined}>
        <div className={styles.main}>{children}</div>
        {sidebar && <aside className={styles.sidebar}>{sidebar}</aside>}
      </div>
    </div>
  )
}
