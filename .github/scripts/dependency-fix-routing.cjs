function eligible(pr, repository) {
  const labels = pr.labels.map(label => label.name);
  return pr.state === 'open' && !pr.draft &&
    pr.user.login === 'dependabot[bot]' && pr.base.ref === 'main' &&
    pr.head.repo?.full_name === repository &&
    /^dependabot\/(nuget|npm_and_yarn)\//.test(pr.head.ref) &&
    labels.includes('dependencies') &&
    !labels.includes('dependency-tier2') &&
    !labels.includes('needs-human-decision');
}

async function findFailingDependency({ github, context }) {
  const repository = `${context.repo.owner}/${context.repo.repo}`;
  const pulls = await github.paginate(github.rest.pulls.list, {
    ...context.repo,
    state: 'open',
    sort: 'created',
    direction: 'asc',
    per_page: 100,
  });

  for (const pr of pulls) {
    if (!eligible(pr, repository)) {
      continue;
    }

    const checks = await github.paginate(github.rest.checks.listForRef, {
      ...context.repo,
      ref: pr.head.sha,
      filter: 'latest',
      per_page: 100,
    });
    const statuses = await github.paginate(github.rest.repos.listCommitStatusesForRef, {
      ...context.repo,
      ref: pr.head.sha,
      per_page: 100,
    });
    const latestStatuses = new Map();
    for (const status of statuses) {
      if (!latestStatuses.has(status.context)) {
        latestStatuses.set(status.context, status.state);
      }
    }

    if (checks.some(check => check.status === 'completed' &&
          ['failure', 'timed_out', 'action_required', 'startup_failure'].includes(check.conclusion)) ||
        [...latestStatuses.values()].some(state => state === 'failure' || state === 'error')) {
      return pr.number;
    }
  }

  return null;
}

module.exports = { findFailingDependency };
