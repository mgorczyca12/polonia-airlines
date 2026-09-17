import { Link } from '@tanstack/react-router'
import { Menu, Button  } from '@mantine/core'
import { IconNavigationCog, IconChecklist, IconPlane } from '@tabler/icons-react'

const Header = () => {
  return (
    <Menu position="bottom-start" width={200}>
      <Menu.Target>
        <Button leftSection={<IconPlane size={20} />}>
          My Trips
        </Button>
      </Menu.Target>

      <Menu.Dropdown >
        <Menu.Item leftSection={<IconNavigationCog size={20} />}>
          Manage Trip
        </Menu.Item>
        <Menu.Item leftSection={<IconChecklist size={20} />}>
          Check-In
        </Menu.Item>
      </Menu.Dropdown>
    </Menu>
  )
}

export default Header


