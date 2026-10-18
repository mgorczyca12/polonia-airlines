import { useNavigate } from '@tanstack/react-router'
import { ActionIcon, Button, Radio, Select, Tabs } from '@mantine/core'
import { DatePickerInput } from '@mantine/dates'
import { useForm } from '@mantine/form'
import {
  ArrowRight,
  ArrowRightLeft,
  BriefcaseBusiness,
  CalendarDays,
  Clock3,
  MapPin,
  PlaneTakeoff,
  Ticket,
} from 'lucide-react'
import { getAirportSelectOptions } from '#/entities/airport'
import { FormProvider } from '../model/form-context'
import type { FlightSearchFormValues } from '../model/form-context'
import {
  DEFAULT_CABIN,
  resolveTravellers,
  resolveTripType,
} from '../model/search-params'
import type { FlightSearchParams } from '../model/search-params'
import styles from '../styles/FlightSearch.module.scss'
import { PassengerDropdown } from './PassengerDropdown'

const airportOptions = getAirportSelectOptions()

const fieldClassNames = {
  label: styles.fieldLabel,
  input: styles.fieldInput,
  section: styles.fieldSection,
}

const UNAVAILABLE_SERVICES = [
  { value: 'manage', label: 'Manage booking', icon: BriefcaseBusiness },
  { value: 'check-in', label: 'Check in', icon: Ticket },
  { value: 'status', label: 'Flight status', icon: Clock3 },
]

interface FlightSearchProps {
  initialSearch?: FlightSearchParams
}

export function FlightSearch({ initialSearch = {} }: FlightSearchProps) {
  const navigate = useNavigate()
  const form = useForm<FlightSearchFormValues>({
    mode: 'controlled',
    initialValues: {
      departureAirportCode: initialSearch.from ?? null,
      arrivalAirportCode: initialSearch.to ?? null,
      departureDate: initialSearch.departure ?? null,
      returnDate: initialSearch.return ?? null,
      tripType: resolveTripType(initialSearch),
      passengers: resolveTravellers(initialSearch),
      cabin: initialSearch.cabin ?? DEFAULT_CABIN,
    },
    validate: {
      arrivalAirportCode: (value, values) =>
        value !== null && value === values.departureAirportCode
          ? 'Choose a destination different from your departure airport'
          : null,
    },
  })
  const { tripType, departureDate, returnDate } = form.values

  const handleSubmit = (values: FlightSearchFormValues) => {
    void navigate({
      to: '/flightlist',
      search: {
        from: values.departureAirportCode ?? undefined,
        to: values.arrivalAirportCode ?? undefined,
        departure: values.departureDate ?? undefined,
        return:
          values.tripType === 'roundTrip'
            ? (values.returnDate ?? undefined)
            : undefined,
        tripType: values.tripType,
        adults: values.passengers.adult,
        children: values.passengers.child,
        infants: values.passengers.infant,
        cabin: values.cabin,
      },
    })
  }

  const handleTripTypeChange = (value: string) => {
    form.setFieldValue('tripType', value === 'oneWay' ? 'oneWay' : 'roundTrip')

    if (value === 'oneWay') {
      form.setFieldValue('returnDate', null)
    }
  }

  const swapAirports = () => {
    form.setValues({
      departureAirportCode: form.values.arrivalAirportCode,
      arrivalAirportCode: form.values.departureAirportCode,
    })
  }

  return (
    <FormProvider form={form}>
      <form
        className={styles.panel}
        onSubmit={form.onSubmit(handleSubmit)}
        aria-label="Search flights"
      >
        <Tabs
          value="book"
          classNames={{
            root: styles.tabs,
            list: styles.tabList,
            tab: styles.tab,
            tabSection: styles.tabSection,
          }}
        >
          <Tabs.List>
            <Tabs.Tab value="book" leftSection={<PlaneTakeoff size={17} />}>
              Book a flight
            </Tabs.Tab>
            {UNAVAILABLE_SERVICES.map(({ value, label, icon: Icon }) => (
              <Tabs.Tab
                key={value}
                value={value}
                leftSection={<Icon size={17} />}
                disabled
                title={`${label} is coming soon`}
              >
                {label}
              </Tabs.Tab>
            ))}
          </Tabs.List>
        </Tabs>

        <Radio.Group
          value={tripType}
          onChange={handleTripTypeChange}
          label="Trip type"
          classNames={{ label: 'polonia-visually-hidden' }}
        >
          <div className={styles.tripTypes}>
            <Radio value="roundTrip" label="Return" size="xs" />
            <Radio value="oneWay" label="One way" size="xs" />
            <Radio
              value="multiCity"
              label="Multi-city"
              size="xs"
              disabled
              title="Multi-city journeys are coming soon"
            />
          </div>
        </Radio.Group>

        <div className={styles.fields}>
          <Select
            label="From"
            placeholder="Departure airport"
            data={airportOptions}
            searchable
            nothingFoundMessage="No airports found"
            rightSection={<MapPin size={17} aria-hidden="true" />}
            rightSectionPointerEvents="none"
            classNames={{ ...fieldClassNames, root: styles.fromField }}
            {...form.getInputProps('departureAirportCode')}
          />

          <ActionIcon
            className={styles.swapButton}
            variant="subtle"
            size={30}
            aria-label="Swap departure and destination airports"
            title="Swap airports"
            onClick={swapAirports}
          >
            <ArrowRightLeft size={19} />
          </ActionIcon>

          <Select
            label="To"
            placeholder="Destination"
            data={airportOptions}
            searchable
            nothingFoundMessage="No airports found"
            rightSection={<MapPin size={17} aria-hidden="true" />}
            rightSectionPointerEvents="none"
            classNames={{ ...fieldClassNames, root: styles.toField }}
            {...form.getInputProps('arrivalAirportCode')}
          />

          {tripType === 'roundTrip' ? (
            <DatePickerInput
              key="return-dates"
              type="range"
              label="Departure — Return"
              placeholder="Choose dates"
              valueFormat="D MMM YYYY"
              labelSeparator="—"
              minDate={new Date()}
              allowSingleDateInRange
              rightSection={<CalendarDays size={17} aria-hidden="true" />}
              rightSectionPointerEvents="none"
              classNames={{ ...fieldClassNames, root: styles.dateField }}
              value={[departureDate, returnDate]}
              onChange={([departure, returnValue]) => {
                form.setFieldValue('departureDate', departure)
                form.setFieldValue('returnDate', returnValue)
              }}
            />
          ) : (
            <DatePickerInput
              key="departure-date"
              label="Departure"
              placeholder="Choose a date"
              valueFormat="D MMM YYYY"
              minDate={new Date()}
              rightSection={<CalendarDays size={17} aria-hidden="true" />}
              rightSectionPointerEvents="none"
              classNames={{ ...fieldClassNames, root: styles.dateField }}
              value={departureDate}
              onChange={(value) => form.setFieldValue('departureDate', value)}
            />
          )}

          <PassengerDropdown
            classNames={{ ...fieldClassNames, root: styles.travellersField }}
          />
        </div>

        <div className={styles.actions}>
          <Button
            type="submit"
            size="lg"
            className={`polonia-action-button ${styles.searchButton}`}
            rightSection={<ArrowRight size={17} />}
          >
            Search flights
          </Button>
        </div>
      </form>
    </FormProvider>
  )
}
