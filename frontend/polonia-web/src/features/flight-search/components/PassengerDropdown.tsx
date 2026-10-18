import {
  ActionIcon,
  Button,
  Combobox,
  InputBase,
  Radio,
  useCombobox,
} from '@mantine/core'
import { ChevronDown, Minus, Plus } from 'lucide-react'
import { useFormContext } from '../model/form-context'
import {
  CABIN_OPTIONS,
  formatTravellers,
  isValidTravellerCounts,
} from '../model/search-params'
import type { TravellerCounts } from '../model/search-params'
import styles from '../styles/PassengerDropdown.module.scss'

const TRAVELLER_TYPES: Array<{
  type: keyof TravellerCounts
  label: string
  description: string
}> = [
  { type: 'adult', label: 'Adults', description: 'Age 12+' },
  { type: 'child', label: 'Children', description: 'Age 2–11' },
  { type: 'infant', label: 'Infants', description: "Under 2, on an adult's lap" },
]

interface PassengerDropdownProps {
  classNames?: Partial<Record<'root' | 'label' | 'input' | 'section', string>>
}

export function PassengerDropdown({ classNames }: PassengerDropdownProps) {
  const form = useFormContext()
  const { passengers, cabin } = form.values
  const combobox = useCombobox({
    onDropdownClose: () => combobox.resetSelectedOption(),
  })

  const nextCounts = (type: keyof TravellerCounts, delta: number): TravellerCounts => ({
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
          label="Travellers & cabin"
          pointer
          rightSection={<ChevronDown size={17} aria-hidden="true" />}
          rightSectionPointerEvents="none"
          classNames={classNames}
          aria-haspopup="dialog"
          aria-expanded={combobox.dropdownOpened}
          onClick={() => combobox.toggleDropdown()}
        >
          {formatTravellers(passengers)} · {cabin}
        </InputBase>
      </Combobox.Target>

      <Combobox.Dropdown>
        <div className={styles.menu} role="group" aria-label="Travellers and cabin">
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

          <Radio.Group
            label="Cabin"
            value={cabin}
            onChange={(value) => {
              const selectedCabin = CABIN_OPTIONS.find((option) => option === value)

              if (selectedCabin) {
                form.setFieldValue('cabin', selectedCabin)
              }
            }}
            classNames={{ label: styles.cabinLabel }}
          >
            <div className={styles.cabinOptions}>
              {CABIN_OPTIONS.map((option) => (
                <Radio key={option} value={option} label={option} size="xs" />
              ))}
            </div>
          </Radio.Group>

          <p className={styles.note}>Up to 8 seated travellers. One infant per adult.</p>

          <Button variant="outline" fullWidth onClick={() => combobox.closeDropdown()}>
            Done
          </Button>
        </div>
      </Combobox.Dropdown>
    </Combobox>
  )
}
