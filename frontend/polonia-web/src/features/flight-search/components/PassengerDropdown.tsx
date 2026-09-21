import { Input, InputBase, Text, Box, Combobox, useCombobox, Button } from '@mantine/core';
import { IconCaretDown, IconCaretUp } from '@tabler/icons-react';
import { useFormContext } from '#/features/flight-search/model/form-context';
import styles from '../styles/PassengerDropdown.module.scss'

const PassengerDropdown = () => {
  const form = useFormContext();
  const { adult, child, infant } = form.values.passengers || { adult: 0, child: 0, infant: 0 };
  const passengersList = Object.keys(form.values.passengers) as Array<keyof typeof form.values.passengers>;;

  const combobox = useCombobox({
    onDropdownClose: () => combobox.resetSelectedOption(),
  });

  // Helper to handle updates
  const updateCount = (type: keyof typeof form.values.passengers, delta: number) => {
    const { adult, child, infant } = form.values.passengers;
    const currentValue = form.values.passengers[type];
    const nextValue = currentValue + delta;

    // 1. Prevent negative numbers
    if (nextValue < 0) return;

    // 2. Max 8 total (Adults + Children)
    if ((type === 'adult' || type === 'child') && delta > 0) {
      if (adult + child >= 8) {
        // Optional: Add a notification or error message here
        return;
      }
    }

    // 3. Max 1 Infant per Adult
    if (type === 'infant' && delta > 0) {
      if (nextValue > adult) {
        // Cannot have more infants than adults
        return;
      }
    }

    // 4. Special Case: If decreasing adults, check if infants exceed new adult count
    if (type === 'adult' && delta < 0) {
      // If reducing an adult would leave more infants than remaining adults, block it
      // Or alternatively: form.setFieldValue('passengers.infant', Math.min(infant, nextValue))
      if (infant > nextValue) {
        return; 
      }
    }

    form.setFieldValue(`passengers.${type}`, nextValue);
  };

  return (
    <Combobox store={combobox} width="400" position="bottom-start">
      <Combobox.Target>
        <InputBase
          component="button"
          type="button"
          pointer
          rightSection={<Combobox.Chevron />}
          onClick={() => combobox.toggleDropdown()}
        >
          {/* Display current counts in the input */}
          {adult + child + infant > 0 
            ? `${adult} Adult, ${child} Child, ${infant} Infant` 
            : <Input.Placeholder>Passengers</Input.Placeholder>}
        </InputBase>
      </Combobox.Target>
      
      <Combobox.Dropdown>
        <Box className={styles.menu}>
          {passengersList.map((type) => (
            <Box className={styles.passengerRow} key={type}>
              <Text size="md" tt="capitalize">{type}</Text>
              <Button.Group>
                <Button variant="default" onClick={() => updateCount(type, -1)}>
                  <IconCaretDown color="var(--mantine-color-red-text)" />
                </Button>
                <Button.GroupSection variant="default" bg="var(--mantine-color-body)" miw={80}>
                  {form.values.passengers[type]}
                </Button.GroupSection>
                <Button 
                  variant="default" 
                  onClick={() => updateCount(type, 1)}
                  disabled={(type === 'infant' && infant >= adult) || ((type === 'adult' || type === 'child') && adult + child >= 8)}
                >
                  <IconCaretUp color="var(--mantine-color-teal-text)" />
                </Button>
              </Button.Group>
            </Box>
          ))}
        </Box>
      </Combobox.Dropdown>
    </Combobox>
  );
};

export default PassengerDropdown