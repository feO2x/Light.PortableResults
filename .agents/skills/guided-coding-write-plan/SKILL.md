---
description: Write a Guided Coding plan or follow-up plan after its approach has been discussed. Run only when explicitly requested by the user.
license: MIT
metadata:
    github-path: skills/guided-coding-write-plan
    github-ref: refs/heads/main
    github-repo: https://github.com/feO2x/guided-coding
    github-tree-sha: a4f2460317872e658ae15c3074140d8f432e75d3
name: guided-coding-write-plan
---
# Write a Plan

Write the agreed approach into `ai-plans/`. Do not use this skill to begin or replace the
planning conversation. Read the repository instructions, including `ai-plans/AGENTS.md`, first.

After writing, report the plan path and stop.

## File name

Use:

```text
YYYY-MM-DD-HHMM-<issue-id>-<kebab-case-description>.md
```

Omit `<issue-id>-` when the work has no tracker issue. Obtain the current UTC timestamp from the
shell; never infer it from conversation context. For example:

```sh
date -u +%F-%H%M
```

In PowerShell, use `(Get-Date).ToUniversalTime().ToString("yyyy-MM-dd-HHmm")`.

Normalize the tracker identifier for filenames: convert it to lowercase, remove a leading `#`,
replace each run of characters other than `a-z` and `0-9` with one hyphen, trim leading and trailing
hyphens, and do not add zero padding. Stop if normalization produces an empty identifier. If the
work requires an issue but none exists, stop and suggest the Guided Coding issue-preparation skill
instead of creating a temporary filename.

Resolve the complete destination path before writing and confirm it does not exist. Never
overwrite or reuse an existing plan or Plan Deviations document; report a collision and stop.

## Structure

Start with `# Title`, followed by exactly these `##` sections in order:

1. `## Rationale`: briefly state the problem and overarching goal.
2. `## Acceptance Criteria`: list observable, verifiable outcomes as unticked Markdown tasks
   (`- [ ]`). Describe results, not implementation steps.
3. `## Technical Details`: record important decisions, constraints, affected components, and
   non-obvious interactions. Give a senior engineer room to implement.

Use minimal code examples only when they clarify an important contract, such as an API
signature, interface, or DTO shape. Say whether an example is exact or illustrative when
unclear. Avoid method bodies, step-by-step instructions, exhaustive file lists, and routine
background.

When behavior changes, require appropriate automated test coverage in the acceptance criteria.
Require benchmarks only when performance is a material risk or requirement.

Discover feedback loops from repository instructions, build files, scripts, and CI configuration.
Look for:

- compilers, type checkers, static analyzers, and linters;
- automated tests, code coverage, and mutation tests;
- benchmarks and performance tests; and
- security scans.

Use the relevant feedback loops to make acceptance criteria verifiable. Do not require every
available loop or claim that tooling exists without confirming it. If the change requires a
missing feedback loop, make adding it explicit in the plan.

## Follow-up plans

Use the same format and issue identifier with a later timestamp. Read every earlier document for
the issue. In the Rationale, name the plans this one follows by exact filename.

Record only the changed decisions and outcomes. State exactly which earlier decisions this plan
supersedes and why. Do not repeat unchanged decisions or contradict earlier plans silently.
