---
description: Decide whether a completed Guided Coding cycle needs a Plan Deviations document and write it when required. Run only when explicitly requested by the user.
license: MIT
metadata:
    github-path: skills/guided-coding-write-deviations
    github-ref: refs/tags/2.0.0
    github-repo: https://github.com/feO2x/guided-coding
    github-tree-sha: 0d828186de7640f8675bdd27783193352ccc46d8
name: guided-coding-write-deviations
---
# Write Plan Deviations

Your goal is to decide whether the completed implementation needs a Plan Deviations document and, if so, to write it into the `ai-plans/` folder at the repository root. After writing, report the document path and stop.

A Plan Deviations document summarizes how the completed implementation differs from the original plan. Reading the initial plan together with this document should be enough to understand the complete work.

## 1. Inspect the Plans and the Implementation

Read all plans for the current ticket in `ai-plans/`, oldest first. Identify them by the `<ticket-id>` or `<short-title>` in their file names, or by the plans mentioned in the conversation. Ask the user if the set of plans is unclear.

Determine the implementation range. Use the PR/MR if there is one. Otherwise, use the merge base between the target branch and the implementation branch. Inspect the commits in that range, all committed, staged, and unstaged changes, and any relevant earlier history. Verify every document and source-file reference you rely on.

## 2. Decide Whether a Document Is Needed

Write a Plan Deviations document in either of these cases:

- **Follow-Up Plans exist.** Summarize the changes from the first plan so that the first plan and this document explain the complete work.
- **The implementation materially changes or rejects an explicit plan decision.** This applies to decisions about at least one Acceptance Criterion, a public contract or data model, an architectural or component boundary, a security or performance constraint, or another design decision future maintainers need to know about.

Routine choices the plans left open are not deviations. An Acceptance Criterion that is documented but not met remains incomplete; it does not count as a deviation unless an accepted Follow-Up Plan explicitly supersedes it.

If there is only one plan and no material deviations, report that conclusion to the user, create nothing, and stop.

## 3. Determine the File Name

The file name is:

- `<timestamp>-<ticket-id>-plan-deviations.md` if a `<ticket-id>` is present
- `<timestamp>-<short-title>-plan-deviations.md` otherwise

Take `<ticket-id>` or `<short-title>` from the file name of the Initial Plan. The `<timestamp>` is UTC in the format `YYYY-MM-DD-HHMM`. Use these commands to get it:

- `date -u +%F-%H%M` on Unix-based shells
- `(Get-Date).ToUniversalTime().ToString("yyyy-MM-dd-HHmm")` on PowerShell

If a plan or Plan Deviations document with this file name already exists, report the collision and stop. Never overwrite or reuse an existing file. Committed Plan Deviations documents are frozen.

## 4. Write the Document

Structure the document as follows:

1. `# <ticket-id or short-title> Plan Deviations` as the title. Prefer `<ticket-id>` if present.
2. An opening paragraph that names every compared plan by its exact file name, names the implementation branch, and identifies plans that were not implemented.
3. `## Summary`: what held up, and how many material decisions changed.
4. `## Changes Across Follow-Up Plans`, only when Follow-Up Plans exist: one numbered `###` section per superseded decision with **Original decision**, **Superseded by** (exact file name and the replacement decision), **Why**, and **Outcome**.
5. `## Deviations From the Accepted Plans`, only when material deviations exist: one numbered `###` section per deviation with **Plan decision** (exact file name and decision), **Implemented**, **Why** (required), and **Impact** (trade-offs, consequences, or deferred work; omit only if there are none).

Name the affected types, members, and files. Leave out work that matches the accepted plans. If the implementation matches all Follow-Up Plans, say so in the Summary and omit `## Deviations From the Accepted Plans`.

## 5. Stop

Report the document path. Do not commit, publish, or create or update a PR/MR. The user reviews and finalizes the document.
