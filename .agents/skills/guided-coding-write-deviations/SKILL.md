---
description: Decide whether a completed Guided Coding cycle needs a Plan Deviations document and write it when required. Run only when explicitly requested by the user.
license: MIT
metadata:
    github-path: skills/guided-coding-write-deviations
    github-ref: refs/heads/main
    github-repo: https://github.com/feO2x/guided-coding
    github-tree-sha: da0a168bf4404594f4c22291bc8f07f3c1f8432c
name: guided-coding-write-deviations
---
# Write Plan Deviations

## Inspect

- Read all plans for the current work, oldest first.
- Determine the implementation range from its PR/MR, or the target branch's merge base with the
  implementation branch.
- Inspect its commits, all committed/staged/unstaged changes, and relevant earlier history. Verify
  every document and source-file reference.

## Decide

Create the Plan Deviation Document if:

- **Follow-up plans exist:** summarize changes from the first plan so it and this document explain
  the complete work.
- **The implementation materially changes or rejects an explicit plan decision** about an
  acceptance outcome; public contract or data model; architectural or component boundary; security
  or performance constraint; or another design decision future maintainers need.

Exclude routine choices the plans left open. A documented but unmet acceptance criterion remains
incomplete unless an accepted follow-up plan explicitly supersedes it.

Otherwise, with one plan and no material deviations, report that conclusion and create nothing.

## Write

Use `YYYY-MM-DD-HHMM-<issue-id>-plan-deviations.md`; without a tracker issue, use
`YYYY-MM-DD-HHMM-<topic>-plan-deviations.md`. Get UTC from the shell, never the conversation:
`date -u +%F-%H%M`, or PowerShell `(Get-Date).ToUniversalTime().ToString("yyyy-MM-dd-HHmm")`.
Normalize issue identifiers like the related plans. Resolve the full unused path. If any plan or
Plan Deviations document already uses it, report the collision and stop; never overwrite or reuse.

Write:

1. `# <issue-id or topic> Plan Deviations`.
2. An opening paragraph naming every compared plan by exact filename and the implementation branch,
   and identifying unimplemented plans.
3. `## Summary`: what held up and how many material decisions changed.
4. `## Changes Across Follow-Up Plans`, when applicable: one numbered `###` per superseded decision
   with **Original decision**, **Superseded by** (exact filename and replacement), **Why**, and
   **Final outcome**.
5. `## Deviations From the Accepted Plans`, when applicable: one numbered `###` per material
   implementation deviation with **Plan decision** (exact filename and decision), **Implemented**,
   **Why** (required), and **Impact** (trade-offs, consequences, or deferred work; omit only if none).

Name affected types, members, and files. Exclude work matching the accepted plans. If all follow-up
plans match, say so in the Summary and omit `## Deviations From the Accepted Plans`.

## Stop

Report the path. Do not commit, publish, or create/update a PR/MR. The user reviews and finalizes it.
