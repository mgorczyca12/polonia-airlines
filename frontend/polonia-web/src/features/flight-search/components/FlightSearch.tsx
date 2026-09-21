import { useNavigate } from '@tanstack/react-router'
import { Box, Paper, Select, Text, Button, SegmentedControl } from '@mantine/core';
import { DatePickerInput } from '@mantine/dates';
import { IconSearch, IconPlaneDeparture, IconPlaneArrival, IconCalendarEvent } from '@tabler/icons-react'
import PassengerDropdown from '#/features/flight-search/components/PassengerDropdown';
import { getAirportSelectOptions } from '#/entities/airport'
import { useForm } from '@mantine/form';
import { FormProvider } from '#/features/flight-search/model/form-context';
import type { FlightSearchFormValues } from '#/features/flight-search/model/form-context'
import styles from '../styles/FlightSearch.module.scss'

const airportOptions = getAirportSelectOptions()

export const FlightSearch = () => {
  const navigate = useNavigate()
  const form = useForm<FlightSearchFormValues>({
      mode: 'controlled',
      initialValues: {
        departureAirportCode: null,
        arrivalAirportCode: null,
        departureDate: null,
        returnDate: null,
        tripType: 'roundTrip', // This is what we are watching
        passengers: {
          adult: 1, 
          child: 0,
          infant: 0
        },
      }
  });

  const handleSubmit = (values: FlightSearchFormValues) => {
    void navigate({
      to: '/flightlist',
      search: {
        from: values.departureAirportCode ?? undefined,
        to: values.arrivalAirportCode ?? undefined,
        departure: values.departureDate?.toISOString().slice(0, 10),
        return: values.returnDate?.toISOString().slice(0, 10),
        tripType: values.tripType,
      },
    })
  };

  const tripType = form.values.tripType

  return (
    <FormProvider form={form}>
      <form onSubmit={form.onSubmit(handleSubmit)}>
        <Paper className={styles.panel}>
          <Box className={styles.content}>
            <Box className={styles.fields}>
              <Text size="xl">Where do you want to go?</Text>
              
              <SegmentedControl
                className={styles.tripToggle}
                data={[
                  { label: 'Round Trip', value: 'roundTrip' },
                  { label: 'One Way', value: 'oneWay' }
                ]}
                {...form.getInputProps('tripType')}
              />

              <Select 
                placeholder='From'
                rightSection={<IconPlaneDeparture />}
                data={airportOptions}
                searchable
                nothingFoundMessage="No airports found..."
                {...form.getInputProps('departureAirportCode')}
              />

              <Select 
                placeholder='To'
                rightSection={<IconPlaneArrival />}
                data={airportOptions}
                searchable
                nothingFoundMessage="No airports found..."
                {...form.getInputProps('arrivalAirportCode')}
              />
              
              <PassengerDropdown />

              <Box className={`${styles.dateFields} ${tripType === 'oneWay' ? styles.oneWayDateFields : ''}`}>
                {tripType === 'oneWay' ? (
                  <DatePickerInput 
                    placeholder="Departure"
                    valueFormat="MM/DD/YYYY"
                    rightSection={<IconCalendarEvent />}
                    key={form.key('departureDate')}
                    {...form.getInputProps('departureDate')}
                  />
                ) : (
                  <>
                    <DatePickerInput 
                      placeholder="Departure"
                      valueFormat="MM/DD/YYYY"
                      rightSection={<IconCalendarEvent />}
                      key={form.key('departureDate')}
                      {...form.getInputProps('departureDate')}
                    />
                    <DatePickerInput 
                      placeholder="Return"
                      valueFormat="MM/DD/YYYY"
                      rightSection={<IconCalendarEvent />}
                      key={form.key('returnDate')}
                      {...form.getInputProps('returnDate')}
                    />
                  </>
                )}
              </Box>
            </Box>
            <Button className={styles.searchButton} fullWidth type="submit" leftSection={<IconSearch size={14}/>}> 
              Search
            </Button>
          </Box>
        </Paper>
      </form>
    </FormProvider>
  )
};