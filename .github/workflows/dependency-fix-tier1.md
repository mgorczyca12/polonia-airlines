---
name: Dependency fixer, tier 1
description: Tier 1 fixer for failing Dependabot PRs. Routes major upgrades and larger repairs to tier 2.
on:
  schedule: every 4h
  workflow_dispatch:
  skip-if-no-match: 'is:pr is:open author:app/dependabot label:dependencies -label:needs-human-decision -label:dependency-tier2'
permissions:
  contents: read
  pull-requests: read
  checks: read
engine:
  id: copilot
  model: gpt-6-luna?effort=low
sandbox:
  agent:
    version: v0.28.62
timeout-minutes: 30
max-turns: 40
max-ai-credits: 100
max-daily-ai-credits: 300
network:
  allowed: [defaults, dotnet, node]
checkout:
  fetch: ["*"]
  fetch-depth: 0
jobs:
  failure_gate:
    runs-on: ubuntu-latest
    permissions:
      contents: read
      pull-requests: read
      checks: read
      statuses: read
    outputs:
      found: ${{ steps.find.outputs.found }}
    steps:
      - uses: actions/checkout@3d3c42e5aac5ba805825da76410c181273ba90b1 # v7.0.1
        with:
          persist-credentials: false
      - id: find
        uses: actions/github-script@3a2844b7e9c422d3c10d287c895573f7108da1b3 # v9.0.0
        with:
          script: |
            const { findFailingDependency } = require('./.github/scripts/dependency-fix-routing.cjs');
            const number = await findFailingDependency({ github, context });
            core.setOutput('found', number !== null ? 'true' : 'false');
            core.info(number === null ? 'No failing dependency PR found.' : `Failing dependency PR: #${number}`);
  agent:
    needs: [failure_gate]
    if: needs.failure_gate.outputs.found == 'true'
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
    required-labels: [dependencies]
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
    allowed: [dependency-tier2, needs-human-decision]
    max: 2
  add-comment:
    max: 1
  dispatch-workflow:
    workflows: [dependency-fix-tier2]
    max: 1
---

{{#runtime-import .github/agents/dependency-fixer.agent.md}}
