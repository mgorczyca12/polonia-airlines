import {
  ActionIcon,
  Button,
  Combobox,
  InputBase,
  useCombobox,
} from '@mantine/core'
import { ChevronDown, Minus, Plus } from 'lucide-react'
import { useFormContext } from '../model/form-context'
import {
  formatTravellers,
  isValidTravellerCounts,
} from '../model/search-params'
import type { TravellerCounts } from '../model/search-params'
import styles from './PassengerDropdown.module.scss'

const TRAVELLER_TYPES: Array<{
  type: keyof TravellerCounts
  label: string
  description: string
}> = [
  { type: 'adult', label: 'Adults', description: 'Age 12+' },
  { type: 'child', label: 'Children', description: 'Age 2–11' },
]

interface PassengerDropdownProps {
  classNames?: Partial<Record<'root' | 'label' | 'input' | 'section', string>>
}

export function PassengerDropdown({ classNames }: PassengerDropdownProps) {
  const form = useFormContext()
  const { passengers } = form.values
  const combobox = useCombobox({
    onDropdownClose: () => combobox.resetSelectedOption(),
  })

  const nextCounts = (
    type: keyof TravellerCounts,
    delta: number,
  ): TravellerCounts => ({
    ...passengers,
    [type]: passengers[type] + delta,
  })

  const canChange = (type: keyof TravellerCounts, delta: number) =>
    isValidTravellerCounts(nextCounts(type, delta))

  const updateCount = (type: keyof TravellerCounts, delta: number) => {
    if (canChange(type, delta)) {
      form.setFieldValue('passengers', nextCounts(type, delta))
    }
  }

  return (
    <Combobox store={combobox} width={340} position="bottom-end" shadow="md">
      <Combobox.Target>
        <InputBase
          component="button"
          type="button"
          label="Travellers"
          pointer
          rightSection={<ChevronDown size={17} aria-hidden="true" />}
          rightSectionPointerEvents="none"
          classNames={classNames}
          aria-haspopup="dialog"
          aria-expanded={combobox.dropdownOpened}
          onClick={() => combobox.toggleDropdown()}
        >
          {formatTravellers(passengers)}
        </InputBase>
      </Combobox.Target>

      <Combobox.Dropdown>
        <div
          className={styles.menu}
          role="group"
          aria-label="Travellers and cabin"
        >
          {TRAVELLER_TYPES.map(({ type, label, description }) => (
            <div className={styles.travellerRow} key={type}>
              <div>
                <p className={styles.travellerLabel}>{label}</p>
                <p className={styles.travellerDescription}>{description}</p>
              </div>
              <div className={styles.stepper}>
                <ActionIcon
                  variant="default"
                  radius="xl"
                  aria-label={`Remove one ${label.toLowerCase()}`}
                  disabled={!canChange(type, -1)}
                  onClick={() => updateCount(type, -1)}
                >
                  <Minus size={14} />
                </ActionIcon>
                <span className={styles.count} aria-live="polite">
                  {passengers[type]}
                </span>
                <ActionIcon
                  variant="default"
                  radius="xl"
                  aria-label={`Add one ${label.toLowerCase()}`}
                  disabled={!canChange(type, 1)}
                  onClick={() => updateCount(type, 1)}
                >
                  <Plus size={14} />
                </ActionIcon>
              </div>
            </div>
          ))}

          <p className={styles.note}>
            Up to 8 travellers aged two or older. Infant bookings are not
            available yet. Choose a cabin from priced fares in the results.
          </p>

          <Button
            variant="outline"
            fullWidth
            onClick={() => combobox.closeDropdown()}
          >
            Done
          </Button>
        </div>
      </Combobox.Dropdown>
    </Combobox>
  )
}
