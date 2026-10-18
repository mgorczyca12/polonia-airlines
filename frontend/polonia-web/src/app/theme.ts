import { createTheme } from '@mantine/core'
import type { MantineColorsTuple } from '@mantine/core'

const poloniaRed: MantineColorsTuple = [
  '#fcedf0',
  '#f6d8dd',
  '#ecaeb9',
  '#e18294',
  '#d85d74',
  '#d2465f',
  '#bd2c45',
  '#a51d35',
  '#8c172c',
  '#721124',
]

export const theme = createTheme({
  primaryColor: 'polonia',
  primaryShade: { light: 7, dark: 6 },
  colors: {
    polonia: poloniaRed,
  },
  fontFamily:
    "'Inter Variable', Inter, system-ui, -apple-system, 'Segoe UI', sans-serif",
  headings: {
    fontFamily: "Lora, Georgia, 'Times New Roman', serif",
    fontWeight: '400',
  },
  defaultRadius: 'sm',
  cursorType: 'pointer',
})
