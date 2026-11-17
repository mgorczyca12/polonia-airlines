const test = require('node:test');
const assert = require('node:assert/strict');
const { findFailingDependency } = require('./dependency-fix-routing.cjs');

const repo = { owner: 'owner', repo: 'repo' };

function pull(number, overrides = {}) {
  return {
    number,
    state: 'open',
    draft: false,
    user: { login: 'dependabot[bot]' },
    base: { ref: 'main' },
    head: { sha: `sha-${number}`, ref: `dependabot/nuget/${number}`, repo: { full_name: 'owner/repo' } },
    labels: [{ name: 'dependencies' }],
    ...overrides,
  };
}

function api(pulls, checks = {}, statuses = {}) {
  const listPulls = () => {};
  const listChecks = () => {};
  const listStatuses = () => {};
  return {
    rest: {
      pulls: { list: listPulls },
      checks: { listForRef: listChecks },
      repos: { listCommitStatusesForRef: listStatuses },
    },
    async paginate(method, options) {
      if (method === listPulls) return pulls;
      if (method === listChecks) return checks[options.ref] ?? [];
      if (method === listStatuses) return statuses[options.ref] ?? [];
      throw new Error('Unexpected API call');
    },
  };
}

test('selects the oldest eligible PR with a failed Actions check', async () => {
  const github = api([pull(1), pull(2), pull(3)], {
    'sha-1': [{ status: 'in_progress', conclusion: null }],
    'sha-2': [{ status: 'completed', conclusion: 'failure' }],
    'sha-3': [{ status: 'completed', conclusion: 'failure' }],
  });
  assert.equal(await findFailingDependency({ github, context: { repo } }), 2);
});

test('uses only the latest status per context and excludes routed or untrusted PRs', async () => {
  const github = api([
    pull(1, { labels: [{ name: 'dependencies' }, { name: 'dependency-tier2' }] }),
    pull(2, { labels: [{ name: 'dependencies' }, { name: 'needs-human-decision' }] }),
    pull(3, { head: { sha: 'sha-3', ref: 'dependabot/nuget/3', repo: { full_name: 'other/repo' } } }),
    pull(4),
    pull(5),
  ], {}, {
    'sha-4': [{ context: 'build', state: 'success' }, { context: 'build', state: 'failure' }],
    'sha-5': [{ context: 'build', state: 'error' }],
  });
  assert.equal(await findFailingDependency({ github, context: { repo } }), 5);
});

test('does not select passing, pending, skipped, or absent checks', async () => {
  const github = api([pull(1), pull(2)], {
    'sha-1': [
      { status: 'completed', conclusion: 'success' },
      { status: 'completed', conclusion: 'skipped' },
    ],
    'sha-2': [{ status: 'in_progress', conclusion: null }],
  }, { 'sha-2': [{ context: 'build', state: 'pending' }] });
  assert.equal(await findFailingDependency({ github, context: { repo } }), null);
});

test('surfaces GitHub API errors', async () => {
  const github = api([pull(1)]);
  github.paginate = async () => { throw new Error('GitHub unavailable'); };
  await assert.rejects(findFailingDependency({ github, context: { repo } }), /GitHub unavailable/);
});
