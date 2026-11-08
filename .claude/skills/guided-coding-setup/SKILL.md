---
description: Set up or upgrade Guided Coding repository instructions, plan storage, and documented feedback-loop commands. Run only when explicitly requested by the user.
license: MIT
metadata:
    github-path: skills/guided-coding-setup
    github-ref: refs/tags/2.0.0
    github-repo: https://github.com/feO2x/guided-coding
    github-tree-sha: f62cf51e978b5b53af1a93a66f6b653e226dc3e9
name: guided-coding-setup
---
# Set Up Guided Coding

Your goal is to set up or upgrade Guided Coding in the current repository. After finishing, report what you changed, give the customization tips below, and stop.

Guided Coding needs these artifacts:

- `AGENTS.md` at the repository root, listing the feedback loops and pointing to `ai-plans/AGENTS.md`.
- `ai-plans/`, the folder holding all plans and Plan Deviations documents.
- `ai-plans/AGENTS.md`, describing the folder, its file naming rules, and how Frozen Plans are treated.

The outcome must be idempotent. Running this skill on a repository sets the artifacts up from scratch, brings outdated ones up to date, or leaves current ones untouched. Content unrelated to Guided Coding, such as project-specific instructions or user-authored notes, is never changed.

## 1. Inspect the Repository

Read the root `AGENTS.md` and `ai-plans/AGENTS.md` if they exist, plus build manifests, task runners, scripts, and CI configuration.

Identify the feedback loops and their exact commands. Look for:

- compilers, type checkers, static analyzers, and linters;
- automated tests, code coverage, and mutation testing tools;
- benchmarks; and
- dependency, secret, container, and source-code security scans.

Only select commands that are immediately executable in the repository. Do not list tools merely because they are common for the detected language.

## 2. Ensure the Root `AGENTS.md`

Create `AGENTS.md` in the repository root if it does not exist. Otherwise, make the smallest update that adds what is missing and corrects what is outdated. Match existing sections by meaning, not by exact heading.

Ensure it contains:

1. `## Feedback Loops`: each command and what it verifies. Report to the user when no feedback loops could be found, and warn that `guided-coding-write-plan` refuses to write plans until at least one is listed.
2. `## Guided Coding`: a link to `ai-plans/AGENTS.md`, noting that it holds the file naming conventions and the rules for working with Frozen Plans. Do not restate those rules here; they live next to the plans they govern, and agents pick them up when they read the folder.

If both sections already exist and are current, leave the file alone.

## 3. Ensure `ai-plans/AGENTS.md`

Create `ai-plans/` if it is missing. Compare `ai-plans/AGENTS.md` with `assets/ai-plans-AGENTS.md` relative to this skill file.

- If the file is missing, copy the asset.
- If the file exists with an older `guided-coding-version` marker or without one, update the Guided Coding paragraphs and the marker. Keep every repository-specific addition, such as a custom plan structure, naming conventions, or notes about legacy file names.

## 4. Report and Give Customization Tips

List the files you created, updated, or left unchanged, the feedback loops you documented, and which commands you ran. Report to the user that they should verify the changes.

Then explain how Guided Coding can be customized, so the user knows the defaults are only a starting point:

- **Plan structure:** `guided-coding-write-plan` uses a default structure with `## Rationale`, `## Acceptance Criteria`, and `## Technical Details`. Describing a different structure in `ai-plans/AGENTS.md` overrides the default.
- **File names:** the skills use the placeholders `<timestamp>` (UTC, `YYYY-MM-DD-HHMM`), `<ticket-id>`, and `<short-title>` (kebab-case, shortened ticket title). Conventions for these, e.g., a `<ticket-id>` format or a maximum length for `<short-title>`, belong in `ai-plans/AGENTS.md`.
- **Feedback loops:** plans should reference the feedback loops in the root `AGENTS.md` to make Acceptance Criteria verifiable. Adding or removing a feedback loop later only requires updating that list.
- **Deviations:** the threshold for what counts as a material deviation can be tightened or relaxed in `ai-plans/AGENTS.md`.

Repository-specific conventions in `AGENTS.md` files take precedence over the skills' defaults.
