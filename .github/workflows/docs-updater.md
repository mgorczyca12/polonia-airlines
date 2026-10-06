---
name: Docs and diagrams updater
description: Keeps docs and Mermaid diagrams in sync after a non-Dependabot PR merges (preview-only, draft PR for human review).
on:
  pull_request_target:
    types: [closed]
    branches: [main]
  workflow_dispatch:
if: >-
  github.event_name == 'workflow_dispatch' ||
  (github.event.pull_request.merged == true &&
   github.event.pull_request.user.login != 'dependabot[bot]')
permissions:
  contents: read
  pull-requests: read
engine:
  id: copilot
  model: gpt-6-luna
sandbox:
  agent:
    version: v0.28.37
timeout-minutes: 20
max-turns: 30
max-ai-credits: 100
max-daily-ai-credits: 300
checkout:
  repository: ${{ github.repository }}
  fetch-depth: 0
tools:
  github:
    toolsets: [pull_requests, repos]
    min-integrity: approved
  bash: ["git:*", "cat", "ls", "grep", "find", "head", "tail"]
safe-outputs:
  staged: true
  report-failure-as-issue: false
  create-pull-request:
    title-prefix: "[docs] "
    labels: [documentation]
    draft: true
    max: 1
    if-no-changes: ignore
    allowed-files:
      - docs/**
      - README.md
      - backend/*/README.md
    protected-files:
      policy: blocked
      exclude: [README.md]
---

{{#runtime-import .github/agents/documentation-writer.agent.md}}
