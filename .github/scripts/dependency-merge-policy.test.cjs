const assert = require('node:assert/strict');
const test = require('node:test');
const { validateReview, validateMerge } = require('./dependency-merge-policy.cjs');

function fixture() {
  const sha = 'a'.repeat(40);
  const context = {
    repo: { owner: 'test-owner', repo: 'test-repo' },
    payload: { inputs: { pr_number: '12', review_run_id: '34', reviewed_head_sha: sha, reviewed_tree_sha: 'c'.repeat(40) } },
  };
  const run = {
    path: '.github/workflows/dependency-fix-tier2.lock.yml',
    event: 'workflow_dispatch',
    head_branch: 'main',
    repository: { full_name: 'test-owner/test-repo' },
    status: 'completed',
    conclusion: 'success',
  };
  const pr = {
    state: 'open',
    draft: false,
    user: { login: 'dependabot[bot]' },
    base: { ref: 'main' },
    head: { sha, ref: 'dependabot/nuget/package-2.0.0', repo: run.repository },
    labels: [{ name: 'dependencies' }, { name: 'dependency-tier2' }],
  };
  const checks = ['backend', 'frontend'].map(name => ({
    name,
    app: { slug: 'github-actions' },
    status: 'completed',
    conclusion: 'success',
  }));
  const files = [{ filename: 'Directory.Packages.props' }];
  const commit = { commit: { tree: { sha: 'c'.repeat(40) } } };
  const comparison = { status: 'ahead' };
  const listForRef = () => {};
  const listFiles = () => {};
  const github = {
    rest: {
      actions: { getWorkflowRun: async () => ({ data: run }) },
      pulls: { get: async () => ({ data: pr }), listFiles },
      checks: { listForRef },
      repos: {
        getCommit: async args => {
          assert.equal(args.ref, pr.head.sha);
          return { data: commit };
        },
        compareCommits: async args => {
          assert.equal(args.base, sha);
          assert.equal(args.head, pr.head.sha);
          return { data: comparison };
        },
      },
    },
    paginate: async (method, args) => {
      if (method === listFiles) {
        assert.equal(args.pull_number, '12');
        return files;
      }
      assert.equal(method, listForRef);
      assert.equal(args.ref, pr.head.sha);
      assert.equal(args.filter, 'latest');
      return checks;
    },
  };
  return { context, github, run, pr, checks, files, commit, comparison };
}

test('accepts a validated no-repair NuGet major upgrade', async () => {
  await validateMerge(fixture());
});

test('accepts a bounded npm repair with tests', async () => {
  const state = fixture();
  state.pr.head.ref = 'dependabot/npm_and_yarn/frontend/polonia-web/package-2.0.0';
  state.files.push({ filename: 'frontend/polonia-web/src/App.test.tsx' });
  await validateMerge(state);
});

for (const [name, change] of [
  ['invalid PR number', s => { s.context.payload.inputs.pr_number = '12; echo unsafe'; }],
  ['invalid review ID', s => { s.context.payload.inputs.review_run_id = '-1'; }],
  ['invalid SHA', s => { s.context.payload.inputs.reviewed_head_sha = 'main'; }],
  ['invalid tree SHA', s => { s.context.payload.inputs.reviewed_tree_sha = 'main'; }],
  ['wrong review workflow', s => { s.run.path = '.github/workflows/ci.yml'; }],
  ['wrong review event', s => { s.run.event = 'pull_request'; }],
  ['untrusted review branch', s => { s.run.head_branch = 'feature'; }],
  ['foreign review repository', s => { s.run.repository = { full_name: 'other/repo' }; }],
  ['pending review outputs', s => { s.run.status = 'in_progress'; }],
  ['failed review outputs', s => { s.run.conclusion = 'failure'; }],
  ['closed PR', s => { s.pr.state = 'closed'; }],
  ['draft PR', s => { s.pr.draft = true; }],
  ['non-Dependabot author', s => { s.pr.user.login = 'someone'; }],
  ['wrong base branch', s => { s.pr.base.ref = 'release'; }],
  ['fork PR', s => { s.pr.head.repo = { full_name: 'fork/test-repo' }; }],
  ['deleted head repository', s => { s.pr.head.repo = null; }],
  ['Actions upgrade', s => { s.pr.head.ref = 'dependabot/github_actions/action-2'; }],
  ['missing dependencies label', s => { s.pr.labels = [{ name: 'dependency-tier2' }]; }],
  ['missing tier 2 label', s => { s.pr.labels = [{ name: 'dependencies' }]; }],
  ['manual decision label', s => { s.pr.labels.push({ name: 'needs-human-decision' }); }],
  ['unreviewed content', s => { s.commit.commit.tree.sha = 'd'.repeat(40); }],
  ['unrelated final head', s => {
    s.pr.head.sha = 'b'.repeat(40);
    s.comparison.status = 'diverged';
  }],
  ['missing CI', s => { s.checks.pop(); }],
  ['failed CI', s => { s.checks[0].conclusion = 'failure'; }],
  ['pending CI', s => { s.checks[0].status = 'in_progress'; }],
  ['skipped CI', s => { s.checks[0].conclusion = 'skipped'; }],
  ['neutral CI', s => { s.checks[0].conclusion = 'neutral'; }],
  ['ambiguous CI producer', s => { s.checks.push({ ...s.checks[0] }); }],
  ['non-Actions CI producer', s => { s.checks[0].app.slug = 'other-app'; }],
  ['missing CI producer', s => { s.checks[0].app = null; }],
  ['nine changed files', s => {
    for (let i = 0; i < 8; i++) s.files.push({ filename: `backend/Api/File${i}.cs` });
  }],
  ['workflow edit', s => { s.files.push({ filename: '.github/workflows/ci.yml' }); }],
  ['domain edit', s => { s.files.push({ filename: 'backend/service/Domain/Entity.cs' }); }],
  ['shared edit', s => { s.files.push({ filename: 'backend/shared/Utility.cs' }); }],
  ['migration', s => { s.files.push({ filename: 'backend/service/Infrastructure/Migrations/Upgrade.cs' }); }],
  ['contract edit', s => { s.files.push({ filename: 'backend/service/Application/Contracts/Response.cs' }); }],
  ['renamed protected file', s => {
    s.files.push({ filename: 'backend/service/Api/Entity.cs', previous_filename: 'backend/service/Domain/Entity.cs' });
  }],
]) {
  test(`rejects ${name}`, async () => {
    const state = fixture();
    change(state);
    await assert.rejects(validateMerge(state));
  });
}

test('allows exactly eight in-scope files', async () => {
  const state = fixture();
  for (let i = 0; i < 7; i++) state.files.push({ filename: `backend/service/Api/File${i}.cs` });
  await validateMerge(state);
});

test('provenance validation permits a running tier 2 run before waiting', async () => {
  const state = fixture();
  state.run.status = 'in_progress';
  state.run.conclusion = null;
  await validateReview(state);
});

test('binds merge to a signed repair commit with the exact validated tree', async () => {
  const state = fixture();
  state.pr.head.sha = 'b'.repeat(40);
  assert.equal(await validateMerge(state), state.pr.head.sha);
});
