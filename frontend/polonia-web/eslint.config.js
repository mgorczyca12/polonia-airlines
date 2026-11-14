//  @ts-check

import { tanstackConfig } from '@tanstack/eslint-config'

const featureInternals = {
  regex: '^#/features/[^/]+/',
  message:
    'Import features through their public index (#/features/<name>), not internal paths.',
}

/** @param {string[]} layers */
function forbidLayers(layers) {
  return layers.map((layer) => ({
    regex: `^#/${layer}(/|$)`,
    message: `This layer must not depend on ${layer}. Dependencies point routes → app/features → entities → shared.`,
  }))
}

export default [
  ...tanstackConfig,
  {
    rules: {
      'import/no-cycle': 'error',
      'import/order': 'off',
      'sort-imports': 'off',
      '@typescript-eslint/array-type': 'off',
      '@typescript-eslint/require-await': 'off',
      'pnpm/json-enforce-catalog': 'off',
      'no-restricted-imports': [
        'error',
        { patterns: [featureInternals, ...forbidLayers(['routes'])] },
      ],
    },
  },
  {
    files: ['src/entities/**'],
    rules: {
      'no-restricted-imports': [
        'error',
        { patterns: forbidLayers(['routes', 'app', 'features']) },
      ],
    },
  },
  {
    files: ['src/shared/**'],
    rules: {
      'no-restricted-imports': [
        'error',
        {
          patterns: forbidLayers(['routes', 'app', 'features', 'entities', 'server']),
        },
      ],
    },
  },
  {
    ignores: ['eslint.config.js', 'prettier.config.js', 'src/routeTree.gen.ts'],
  },
]
