import { createFileRoute } from '@tanstack/react-router';
import { FlightSearch } from '../components/FlightSearch'
import { FlightListing } from '#/components/FlightListing';
import { Space } from '@mantine/core';

const App = () => {

  return (
    <>
      <FlightSearch />
      <Space h="md"/>
      <FlightListing />
    </>
  );
}

export const Route = createFileRoute('/')({ component: App })
