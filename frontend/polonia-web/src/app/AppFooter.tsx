import styles from './AppFooter.module.scss'

export function AppFooter() {
  return (
    <footer className={styles.footer}>
      <div className={styles.inner}>
        <p className={styles.text}>
          © {new Date().getFullYear()} Polonia Airlines.
        </p>
        <p className={styles.text}>Plan, select and book your journey.</p>
      </div>
    </footer>
  )
}
