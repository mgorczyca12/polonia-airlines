---
name: Test writer
description: Writes unit, integration and Playwright tests for a PR when the write-tests label is applied (preview-only).
on:
  label_command: write-tests
permissions:
  contents: read
  pull-requests: read
engine:
  id: copilot
  model: gpt-6.1-sol
sandbox:
  agent:
    version: v0.28.62
timeout-minutes: 40
concurrency:
  job-discriminator: ${{ github.run_id }}
max-turns: 60
max-ai-credits: 200
max-daily-ai-credits: 400
network:
  allowed: [defaults, dotnet, node]
checkout:
  fetch: ["*"]
  fetch-depth: 0
steps:
  - uses: actions/setup-dotnet@0f4a354dd5547a28c70b7853dbd95d5b67886e73 # v6.1.0
    with:
      dotnet-version: 10.0.x
  - uses: pnpm/action-setup@73e46992e05b19abd31f2f3c20e0e7370d762115 # v6.1.1
    with:
      version: 12
  - uses: actions/setup-node@949feb2413d6458794dcd2491c4babbbce0c15c1 # v7.1.0
    with:
      node-version: 26
tools:
  github:
    toolsets: [pull_requests, repos]
    min-integrity: approved
  bash: ["dotnet:*", "pnpm:*", "git:*", "cat", "ls", "grep", "find", "head", "tail"]
safe-outputs:
  staged: true
  report-failure-as-issue: false
  push-to-pull-request-branch:
    target: "*"
    allowed-files:
      - backend/tests/**
      - frontend/polonia-web/src/**/*.test.ts
      - frontend/polonia-web/src/**/*.test.tsx
      - frontend/polonia-web/e2e/**
  add-labels:
    allowed: [needs-human-decision]
    max: 1
  add-comment:
    max: 1
---

{{#runtime-import .github/agents/test-writer.agent.md}}
