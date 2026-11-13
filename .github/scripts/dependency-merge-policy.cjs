function inputs(context) {
  const values = context.payload.inputs;
  if (!/^[1-9]\d*$/.test(values.pr_number) ||
      !/^[1-9]\d*$/.test(values.review_run_id) ||
      !/^[0-9a-f]{40}$/.test(values.reviewed_head_sha) ||
      !/^[0-9a-f]{40}$/.test(values.reviewed_tree_sha)) {
    throw new Error('Invalid PR number, review run ID, reviewed head SHA, or tree SHA.');
  }
  return values;
}

async function validateReview({ github, context }) {
  const values = inputs(context);
  const { data: run } = await github.rest.actions.getWorkflowRun({
    ...context.repo,
    run_id: values.review_run_id,
  });
  if (run.path !== '.github/workflows/dependency-fix-tier2.lock.yml' ||
      run.event !== 'workflow_dispatch' ||
      run.head_branch !== 'main' ||
      run.repository.full_name !== `${context.repo.owner}/${context.repo.repo}`) {
    throw new Error('The merge request must reference a tier 2 dispatch run on main in this repository.');
  }
}

async function validateMerge({ github, context }) {
  const values = inputs(context);
  await validateReview({ github, context });
  const { data: run } = await github.rest.actions.getWorkflowRun({
    ...context.repo,
    run_id: values.review_run_id,
  });
  if (run.status !== 'completed' || run.conclusion !== 'success') {
    throw new Error('Tier 2 review and its safe outputs must complete successfully before merging.');
  }

  const { data: pr } = await github.rest.pulls.get({
    ...context.repo,
    pull_number: values.pr_number,
  });
  const labels = pr.labels.map(label => label.name);
  if (pr.state !== 'open' || pr.draft || pr.user.login !== 'dependabot[bot]' ||
      pr.base.ref !== 'main' ||
      pr.head.repo?.full_name !== `${context.repo.owner}/${context.repo.repo}` ||
      !/^dependabot\/(nuget|npm_and_yarn)\//.test(pr.head.ref) ||
      !labels.includes('dependencies') || !labels.includes('dependency-tier2') ||
      labels.includes('needs-human-decision')) {
    throw new Error('PR is not eligible for tier 2 dependency auto-merge.');
  }
  const { data: commit } = await github.rest.repos.getCommit({
    ...context.repo,
    ref: pr.head.sha,
  });
  if (commit.commit.tree.sha !== values.reviewed_tree_sha) {
    throw new Error('PR source tree changed after tier 2 validation; a new review is required.');
  }
  if (pr.head.sha !== values.reviewed_head_sha) {
    const { data: comparison } = await github.rest.repos.compareCommits({
      ...context.repo,
      base: values.reviewed_head_sha,
      head: pr.head.sha,
    });
    if (comparison.status !== 'ahead') {
      throw new Error('The final PR head must descend from the original reviewed head.');
    }
  }

  const files = await github.paginate(github.rest.pulls.listFiles, {
    ...context.repo,
    pull_number: values.pr_number,
    per_page: 100,
  });
  const allowedFile = /^(Directory\.Packages\.props|frontend\/polonia-web\/(package\.json|pnpm-lock\.yaml|src\/.+)|backend\/.+\.(cs|csproj))$/;
  const humanOnlyFile = /(^|\/)(Domain|Migrations|Contracts)\//;
  if (files.length > 8 || files.some(file =>
    !allowedFile.test(file.filename) || humanOnlyFile.test(file.filename) ||
    file.filename.startsWith('backend/shared/') ||
    (file.previous_filename && (!allowedFile.test(file.previous_filename) ||
      humanOnlyFile.test(file.previous_filename) ||
      file.previous_filename.startsWith('backend/shared/'))))) {
    throw new Error('PR exceeds the bounded repair scope; manual review is required.');
  }

  const checks = await github.paginate(github.rest.checks.listForRef, {
    ...context.repo,
    ref: pr.head.sha,
    filter: 'latest',
    per_page: 100,
  });
  for (const name of ['backend', 'frontend']) {
    const matches = checks.filter(check => check.name === name);
    if (matches.length !== 1 || matches[0].app?.slug !== 'github-actions' ||
        matches[0].status !== 'completed' || matches[0].conclusion !== 'success') {
      throw new Error(`Mandatory ${name} CI must succeed on the reviewed commit with an unambiguous GitHub Actions producer.`);
    }
  }
  return pr.head.sha;
}

module.exports = { validateReview, validateMerge };
