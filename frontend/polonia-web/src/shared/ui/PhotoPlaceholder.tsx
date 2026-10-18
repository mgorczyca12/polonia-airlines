import { ImageIcon } from 'lucide-react'
import styles from './PhotoPlaceholder.module.scss'

interface PhotoPlaceholderProps {
  className?: string
  label?: string
}

/** Neutral stand-in for destination photography until licensed images are supplied. */
export function PhotoPlaceholder({ className, label }: PhotoPlaceholderProps) {
  return (
    <div className={`${styles.placeholder} ${className ?? ''}`} aria-hidden="true">
      <ImageIcon size={28} strokeWidth={1.5} />
      {label && <span className={styles.label}>{label}</span>}
    </div>
  )
}
