---
description: Set up or upgrade Guided Coding repository instructions, plan storage, and documented feedback-loop commands. Run only when explicitly requested by the user.
license: MIT
metadata:
    github-path: skills/guided-coding-setup
    github-ref: refs/heads/main
    github-repo: https://github.com/feO2x/guided-coding
    github-tree-sha: 45dc5336407039c7a5c1422eb37ad7069a11724f
name: guided-coding-setup
---
# Set Up Guided Coding

Set up or upgrade Guided Coding in the current repository. Preserve project-specific instructions
and user-authored content. Never modify existing plan or Plan Deviations documents.

## 1. Inspect the repository

Read the root `AGENTS.md` when present, `ai-plans/AGENTS.md` when present, nested instruction
files, build manifests, task runners, scripts, and CI configuration.

Identify feedback loops and their exact commands. Look for:

- compilers, transpilers, type checkers, static analyzers, and linters;
- unit, integration, end-to-end, coverage, and mutation tests;
- benchmarks and performance tests; and
- dependency, secret, container, and source-code security scans.

Do not invent commands or list tools merely because they are common for the detected language.
Prefer the repository's documented entry points. When a command is safe, non-destructive, and
reasonably bounded, run it to detect stale instructions. Do not claim that a command passed unless
it was executed successfully; report why any documented command was not run. If existing Guided
Coding instructions conflict with this version and resolving them would discard a project-specific
decision, ask before editing.

## 2. Update the root instructions

Create `AGENTS.md` if it does not exist. Otherwise make the smallest idempotent update that
preserves unrelated sections.

Ensure it contains:

1. `## When you implement a plan`, stating that:
   - plans in `ai-plans/` are frozen after their Planning Phase;
   - only acceptance criteria may change, from `- [ ]` to `- [x]`;
   - a criterion is checked only after the implementation and relevant feedback loops verify it;
   - unmet criteria remain unchecked and their wording is never changed; and
   - material departures from explicit plan decisions require a Plan Deviations document rather
     than edits to the frozen plan.
2. `## Feedback loops`, listing each repository-confirmed command and what it verifies. State
   plainly when no automated feedback-loop command can be confirmed.
3. `## How to write plans`, linking to `ai-plans/AGENTS.md`.
4. `## This is your space` as the final section, inviting agents to record noteworthy repository
   discoveries for later discussion. Preserve any notes already in that section.

Consolidate equivalent existing sections instead of adding duplicates.

## 3. Update the plan record

Create `ai-plans/` when missing. Create or update `ai-plans/AGENTS.md` from
`assets/ai-plans-AGENTS.md`.

Keep the version marker and portable Guided Coding rules current while preserving repository-
specific additions and notes about legacy plan filenames. Do not rename, rewrite, or reorganize
historical documents during an upgrade.

## 4. Report

Report the files created or updated, the feedback loops documented, which commands were executed
and their results, why any were not run, and any unresolved conflicts. Then stop.
