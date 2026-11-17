---
name: Dependency fixer, tier 2
description: Reviews and repairs major Dependabot upgrades and tier 1 escalations; requests guarded auto-merge or human review.
on:
  workflow_dispatch:
    inputs:
      pr_number:
        description: Dependabot pull request number
        required: true
        type: string
      summary:
        description: Routing reason or what tier 1 tried
        required: false
        type: string
permissions:
  contents: read
  pull-requests: read
  checks: read
engine:
  id: copilot
  model: gpt-6.1-sol
sandbox:
  agent:
    version: v0.28.62
timeout-minutes: 45
concurrency:
  job-discriminator: ${{ github.event.inputs.pr_number }}
max-turns: 60
max-ai-credits: 300
max-daily-ai-credits: 600
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
  report-failure-as-issue: false
  push-to-pull-request-branch:
    target: "*"
    required-labels: [dependencies, dependency-tier2]
    protected-files:
      policy: allowed
      exclude: [Directory.Packages.props, package.json, pnpm-lock.yaml]
    allowed-files:
      - Directory.Packages.props
      - frontend/polonia-web/package.json
      - frontend/polonia-web/pnpm-lock.yaml
      - backend/**/*.cs
      - backend/**/*.csproj
      - frontend/polonia-web/src/**
  add-labels:
    allowed: [needs-human-decision]
    target: ${{ github.event.inputs.pr_number }}
    required-labels: [dependencies, dependency-tier2]
    max: 1
  add-comment:
    target: ${{ github.event.inputs.pr_number }}
    required-labels: [dependencies, dependency-tier2]
    max: 1
  dispatch-workflow:
    workflows: [dependency-automerge]
    max: 1
  create-issue:
    title-prefix: "[decision] "
    labels: [needs-human-decision]
    max: 1
---

{{#runtime-import .github/agents/dependency-fixer-tier2.agent.md}}
