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
  - uses: actions/setup-dotnet@a98b56852c35b8e3190ac28c8c2271da59106c68 # v6.0.0
    with:
      dotnet-version: 10.0.x
  - uses: pnpm/action-setup@ea17c68df8912ef543352723c149a84f56e3d413 # v6.1.0
    with:
      version: 12
  - uses: actions/setup-node@820762786026740c76f36085b0efc47a31fe5020 # v7.0.0
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
