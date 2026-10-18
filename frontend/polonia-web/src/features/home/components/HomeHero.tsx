import type { ReactNode } from 'react'
import styles from '../styles/HomeHero.module.scss'

interface HomeHeroProps {
  children: ReactNode
}

export function HomeHero({ children }: HomeHeroProps) {
  return (
    <section className={styles.hero} aria-labelledby="home-hero-title">
      {/* Neutral backdrop until Polonia destination photography is supplied. */}
      <div className={styles.backdrop} aria-hidden="true" />
      <div className={styles.content}>
        <div className={styles.story}>
          <p className={styles.eyebrow}>Extraordinary begins with Polonia</p>
          <h1 id="home-hero-title" className={styles.title}>
            The world is yours.
            <br />
            Make it a journey.
          </h1>
          <p className={styles.subtitle}>
            Thoughtful journeys. Remarkable places. A warmer welcome.
          </p>
        </div>
        {children}
      </div>
    </section>
  )
}
