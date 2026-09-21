import { Box, Button, Card, Text, TextInput } from '@mantine/core'
import { useForm } from '@mantine/form'
import { useState } from 'react'
import type { PassengerDetails } from '../model/booking.types'

export function PassengerDetailsForm() {
  const [confirmed, setConfirmed] = useState(false)
  const form = useForm<PassengerDetails>({
    mode: 'controlled',
    initialValues: {
      firstName: '',
      lastName: '',
      email: '',
    },
    validate: {
      firstName: (value) => value.trim() ? null : 'First name is required',
      lastName: (value) => value.trim() ? null : 'Last name is required',
      email: (value) => /^\S+@\S+$/.test(value) ? null : 'Enter a valid email address',
    },
  })

  const handleSubmit = (values: PassengerDetails) => {
    setConfirmed(true)
  }

  return (
    <Card withBorder p="lg">
      <form onSubmit={form.onSubmit(handleSubmit)}>
        <Box>
          <Box>
            <Text fw={600}>Passenger details</Text>
            <Text size="sm" c="dimmed">Enter the primary passenger's contact information.</Text>
          </Box>
          <Box>
            <TextInput label="First name" required {...form.getInputProps('firstName')} />
            <TextInput label="Last name" required {...form.getInputProps('lastName')} />
          </Box>
          <TextInput label="Email address" type="email" required {...form.getInputProps('email')} />
          <Button type="submit">Confirm booking</Button>
          {confirmed && <Text c="green.6" size="sm">Booking details confirmed for this session.</Text>}
        </Box>
      </form>
    </Card>
  )
}