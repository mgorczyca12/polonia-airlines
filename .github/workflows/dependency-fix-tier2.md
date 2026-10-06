---
name: Dependency fixer, tier 2
description: Tier 2 fixer for Dependabot PRs escalated by tier 1 (preview-only). Escalates to a human.
on:
  workflow_dispatch:
    inputs:
      pr_number:
        description: Dependabot pull request number
        required: true
        type: string
      summary:
        description: What tier 1 tried
        required: false
        type: string
permissions:
  contents: read
  pull-requests: read
  checks: read
engine:
  id: copilot
  model: gpt-6-astra
sandbox:
  agent:
    version: v0.28.37
timeout-minutes: 45
concurrency:
  job-discriminator: ${{ github.run_id }}
max-turns: 60
max-ai-credits: 300
max-daily-ai-credits: 600
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
    max: 1
  add-comment:
    max: 1
  create-issue:
    title-prefix: "[decision] "
    labels: [needs-human-decision]
    max: 1
---

{{#runtime-import .github/agents/dependency-fixer-tier2.agent.md}}
