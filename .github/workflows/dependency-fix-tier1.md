---
name: Dependency fixer, tier 1
description: Tier 1 fixer for failing Dependabot PRs (preview-only). Escalates to tier 2, then to a human.
on:
  schedule: every 4h
  workflow_dispatch:
  skip-if-no-match: 'is:pr is:open author:app/dependabot label:dependencies status:failure -label:needs-human-decision -label:dependency-tier2'
permissions:
  contents: read
  pull-requests: read
  checks: read
engine:
  id: copilot
  model: gpt-6-luna?effort=low
sandbox:
  agent:
    version: v0.28.37
timeout-minutes: 30
max-turns: 40
max-ai-credits: 100
max-daily-ai-credits: 300
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
