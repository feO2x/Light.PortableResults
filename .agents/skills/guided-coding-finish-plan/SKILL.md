---
description: Validate and freeze a Guided Coding plan, then optionally publish it to its tracker issue. Run only when explicitly requested by the user.
license: MIT
metadata:
    github-path: skills/guided-coding-finish-plan
    github-ref: refs/heads/main
    github-repo: https://github.com/feO2x/guided-coding
    github-tree-sha: 991dbf3c4edc54ba093457a75a53b23a52e9501b
name: guided-coding-finish-plan
---
# Finish a Plan

Finish the plan named by the user. If none is named, proceed only when exactly one uncommitted plan
draft exists in `ai-plans/`; otherwise ask for its path.

## 1. Validate

Read the repository instructions and confirm that:

- The filename is either
  `YYYY-MM-DD-HHMM-<issue-id>-<kebab-case-description>.md` or, without an issue,
  `YYYY-MM-DD-HHMM-<kebab-case-description>.md`.
- Variable filename segments contain only lowercase ASCII letters, digits, and single hyphens, and
  do not start or end with a hyphen.
- The file starts with `# Title`, followed by exactly `## Rationale`,
  `## Acceptance Criteria`, and `## Technical Details`, in that order.
- Every acceptance criterion is an unticked task (`- [ ]`).
- Referenced plan documents exist, and claims about existing source files are accurate. Paths for
  files the plan intends to create are valid references when identified as planned work.

Report validation failures. Fix them only after the user agrees; the Planning Phase is still open
until the plan is committed.

## 2. Commit and freeze

Inspect `git status` and the staged diff. Preserve unrelated working-tree and staged changes. Stage
the plan if needed, then use a path-limited commit so the commit contains only the plan file. Verify
the resulting commit's file list before continuing; if it contains anything else, stop and report
the problem without rewriting history. Follow repository commit conventions and do not push. The
successful commit ends the Planning Phase and freezes the plan.

## 3. Optionally publish the first plan

The first plan for a tracked issue may become that issue's description. Follow-up plans are not
published there.

Use the tracker and target project documented by the repository. If neither is documented, infer
them only when the git remote and tracker clearly agree, state the inferred target, and ask the
user to confirm it. If the project has no tracker, skip this step.

Read the current issue title and description before asking to publish. Explain that publishing
replaces the complete issue description. If it is non-empty, show or summarize what would be
replaced and require explicit confirmation to overwrite it. Publish the plan body only. For GitHub:

```sh
gh issue edit <issue-id> --body-file <plan-path>
```

If the plan and issue titles differ, report it without renaming either. Treat the tracker body as
a publication snapshot, never as the source from which the committed plan is amended. Never change
the tracker without the user's approval.

## 4. Report

Report the committed plan path and state that its Planning Phase has ended and it is now frozen.
