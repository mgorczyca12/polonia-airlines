import { Headset, Luggage, Utensils } from 'lucide-react'
import styles from '../styles/ServicePromises.module.scss'

const PROMISES = [
  {
    icon: Luggage,
    title: 'Room for your journey',
    description: 'Fares from light to fully flexible',
  },
  {
    icon: Utensils,
    title: 'A taste of Poland',
    description: 'Regionally inspired dining on board',
  },
  {
    icon: Headset,
    title: 'Here when you need us',
    description: 'Clear guidance at every step of booking',
  },
]

export function ServicePromises() {
  return (
    <section className={styles.promises} aria-label="Why fly with Polonia">
      <ul className={styles.list}>
        {PROMISES.map(({ icon: Icon, title, description }) => (
          <li key={title} className={styles.promise}>
            <Icon className={styles.icon} size={26} strokeWidth={1.5} aria-hidden="true" />
            <div>
              <p className={styles.title}>{title}</p>
              <p className={styles.description}>{description}</p>
            </div>
          </li>
        ))}
      </ul>
    </section>
  )
}
