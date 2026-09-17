import { useState } from 'react';
import { Paper, Select, Text, Button, Stack, SegmentedControl, SimpleGrid } from '@mantine/core';
import { DatePickerInput } from '@mantine/dates';
import { IconSearch, IconPlaneDeparture, IconPlaneArrival, IconCalendarEvent } from '@tabler/icons-react'
import PassengerDropdown from '#/components/FlightSearch/PassengerDropdown';
import airportData from '#/data/airports.json'
import { useForm } from '@mantine/form';
import { FormProvider } from '#/components/FlightSearch/form-context';

const data = airportData.map((item) => ({
    // 'value' is what is actually selected/stored
    value: item.Code,
    label: `${item.City} ${item.Airport} (${item.Code})`
  }));

export const FlightSearch = () => {
  const form = useForm({
      mode: 'uncontrolled',
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

  const handleSubmit = (values: typeof form.values) => {
    console.log('Form Submitted:', values);
    // Your API call or logic here
  };

  const [tripType, setTripType] = useState('roundTrip');
  // This "subscribes" the component to changes in tripType 
  // so the ternary logic below will re-evaluate.
  form.watch('tripType', ({ value }) => setTripType(value));

  return (
    <FormProvider form={form}>
      <form onSubmit={form.onSubmit(handleSubmit)}>
        <Paper bg="dark.5" shadow="sm" p="xl" w={620}>
          <Stack>
            <SimpleGrid cols={2}>
              <Text size="xl">Where do you want to go?</Text>
              
              <SegmentedControl
                data={[
                  { label: 'Round Trip', value: 'roundTrip' },
                  { label: 'One Way', value: 'oneWay' }
                ]}
                {...form.getInputProps('tripType')}
              />

              <Select 
                placeholder='From'
                rightSection={<IconPlaneDeparture />}
                data={data}
                searchable
                nothingFoundMessage="No airports found..."
                {...form.getInputProps('departureAirportCode')}
              />

              <Select 
                placeholder='To'
                rightSection={<IconPlaneArrival />}
                data={data}
                searchable
                nothingFoundMessage="No airports found..."
                {...form.getInputProps('arrivalAirportCode')}
              />
              
              <PassengerDropdown />

              {tripType === 'oneWay' ? (
                <DatePickerInput 
                  placeholder="Departure"
                  valueFormat="MM/DD/YYYY"
                  rightSection={<IconCalendarEvent />}
                  key={form.key('departureDate')}
                  {...form.getInputProps('departureDate')}
                />
              ) : (
                <SimpleGrid cols={2} p=''>
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
                </SimpleGrid>
              )}
            </SimpleGrid>
            <Button fullWidth type="submit" leftSection={<IconSearch size={14}/>}>
              Search
            </Button>
          </Stack>
        </Paper>
      </form>
    </FormProvider>
  )
};