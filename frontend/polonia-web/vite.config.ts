import { defineConfig } from 'vite'
import { devtools } from '@tanstack/devtools-vite'

import { tanstackStart } from '@tanstack/react-start/plugin/vite'

import viteReact from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

const config = defineConfig({
  resolve: { tsconfigPaths: true },
  plugins: [
    tanstackStart(),
    devtools({
      // The root document is annotated with different line numbers by the
      // server and client builds, which causes hydration mismatches.
      injectSource: { enabled: true, ignore: { files: [/routes[\\/]__root\.tsx$/] } },
      // Forwarding logs both ways between browser and dev server re-logged
      // errors in a growing loop that froze the page.
      consolePiping: { enabled: false },
    }),
    tailwindcss(), 
    viteReact()
  ],
})

export default config
