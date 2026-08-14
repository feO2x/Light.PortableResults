---
description: Create an empty tracker issue and a clean local branch for a Guided Coding plan. Run only when explicitly requested by the user.
license: MIT
metadata:
    github-path: skills/guided-coding-prepare-issue-for-plan
    github-ref: refs/heads/main
    github-repo: https://github.com/feO2x/guided-coding
    github-tree-sha: 67b76327f9be129189afe9d8390f64044d3d69c5
name: guided-coding-prepare-issue-for-plan
---
# Prepare an Issue for a Plan

Create the issue and branch only. Leave planning to the user and the planning conversation.

## 1. Check prerequisites

Read the repository instructions. Before creating the issue:

1. Run `git status`. If the worktree has uncommitted changes, stop before creating the issue.
2. Determine the repository's default branch.
3. Determine the issue tracker and target project from repository instructions. If they are not
   documented, infer them only when the git remote and tracker clearly agree, such as `gh` with a
   GitHub remote. State the inferred target and ask the user to confirm it.
4. When a tracker exists, confirm that its CLI is available and authenticated for the target
   project. Check its help rather than guessing flags.

Use the title supplied by the user. If none is provided, ask for a short title. Derive a lowercase,
hyphen-separated topic of at most four words.

Example: `Support cancelled events` becomes `cancelled-events`.

## 2. Update the default branch

Switch to the default branch and update it before creating the external issue:

```sh
git switch <default-branch>
git pull --ff-only
```

Stop if either command fails.

## 3. Create the issue

Create the issue with the agreed title and an empty description. Do not add a summary,
acceptance criteria, or placeholder text. Use the tracker's documented CLI. For GitHub:

```sh
gh issue create --title "<title>" --body ""
```

Read the identifier and URL from the command output. Normalize the identifier for filenames and
branches: convert it to lowercase, remove a leading `#`, replace each run of characters other than
`a-z` and `0-9` with one hyphen, trim leading and trailing hyphens, and do not add zero padding. Stop
if normalization produces an empty identifier.

If the project has no issue tracker, skip issue creation and use the topic alone for the branch
and later plan filename.

## 4. Create the branch

Create `<issue-id>-<topic>` or, without an issue, `<topic>`. Validate the complete name before
creating it:

```sh
git check-ref-format --branch "<branch-name>"
git switch -c <branch-name>
```

Stop if the intended branch already exists; do not reuse or rename it implicitly. If validation or
branch creation fails after the issue was created, do not close or delete the issue automatically.
Report the issue and the failure so the user can choose the recovery action.

## 5. Report

Report the issue identifier and URL, when present, and the branch name. Then stop. Do not write
the plan.
