import { Card, Collapse, Divider, Group, Stack, Text, UnstyledButton } from "@mantine/core";
import { useDisclosure } from "@mantine/hooks";

export const FlightListing = () => {
  const [expanded, { toggle }] = useDisclosure(false);

  return(
    <Card withBorder p="md" w={1000}>
      <Stack>
        <Group justify="space-between" wrap="nowrap">
          <Group style={{ flex: 1 }} wrap="nowrap" pr="xl">
            <Text style={{ whiteSpace: 'nowrap' }}>6:20 PM: ORD</Text>
            <Divider
              style={{ flex: 1 }}
              labelPosition="center"
              label={
                <Stack justify="center" gap={1}>
                  <Text size="sm">Duration</Text>
                  <Text size="sm">7h 30m</Text>
                </Stack>
              }
            />
            <Text style={{ whiteSpace: 'nowrap' }}>11:20 AM: KRK</Text>
          </Group>
          <Group>
            <UnstyledButton onClick={toggle}>
              <Card withBorder orientation="vertical" p="xs" w={100} h={150}>
                <Text>Economy</Text>
              </Card>
            </UnstyledButton>
            <Card withBorder orientation="vertical" p="xs" w={100} h={150}>
              <Text>Premium</Text>
            </Card>
            <Card withBorder orientation="vertical" p="xs" w={100} h={150}>
              <Text>Business</Text>
            </Card>
            <Card withBorder orientation="vertical" p="xs" w={100} h={150}>
              <Text>First Class</Text>
            </Card>
          </Group>
        </Group>
        <Collapse expanded={expanded}>
          <Group justify="center">
            <Card withBorder orientation="vertical" p="xs" w={250} h={300}>
              <Text>Light</Text>
            </Card>
            <Card withBorder orientation="vertical" p="xs" w={250} h={300}>
              <Text>Standard</Text>
            </Card>
            <Card withBorder orientation="vertical" p="xs" w={250} h={300}>
              <Text>Full</Text>
            </Card>
          </Group>
        </Collapse>
      </Stack>
    </Card>
  );
}