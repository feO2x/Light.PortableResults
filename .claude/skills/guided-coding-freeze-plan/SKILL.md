---
description: Freeze a Guided Coding plan. Run only when explicitly requested by the user.
license: MIT
metadata:
    github-path: skills/guided-coding-freeze-plan
    github-ref: refs/tags/2.0.0
    github-repo: https://github.com/feO2x/guided-coding
    github-tree-sha: c4c870c20ecf9b95419fd7e787553b18b5591b8b
name: guided-coding-freeze-plan
---
# Freeze a Plan

Freezing a plan means providing a timestamp, both at the beginning of its file name and directly after its title. An optional `<ticket-id>` is also inserted into the file name after the timestamp.

The plan is then considered ready for implementation, only its acceptance criteria are allowed to be checked off by the implementer.

The target plan should be already mentioned in the conversation, typically in the format `<short-title>.md`. If not, first inspect the `ai-plans/` folder for a single plan Markdown file which is untracked or has changes. Otherwise, ask the user to provide the path to the plan file.

If the plan already carries a timestamp in its file name or a `*Frozen at ...*` line below its title, it is frozen. Report this to the user and stop.

## 1. Determine the Timestamp

The `<timestamp>` is UTC in the format `YYYY-MM-DD-HHMM`. Use these commands to get it:

- `date -u +%F-%H%M` on Unix-based shells
- `(Get-Date).ToUniversalTime().ToString("yyyy-MM-dd-HHmm")` on PowerShell

## 2. Determine an Optional Ticket ID

Plans are typically associated with a `<ticket-id>`. This is the ID of a work item such as a GitHub issue or a Jira task. If you don't know it from the conversation yet, ask the user to provide it. Note that not each plan needs to be associated with a `<ticket-id>`.

## 3. Change the Plan File Name

- `<timestamp>-<ticket-id>-<short-title>.md` if `<ticket-id>` is present
- `<timestamp>-<short-title>.md` otherwise

If a file with this name already exists in `ai-plans/`, report the collision and stop. Never overwrite an existing file.

## 4. Stamp the Plan Content

Right beneath the `# Title`, insert the following line: `*Frozen at <timestamp-utc>*`, where `<timestamp-utc>` is an ISO 8601 timestamp of the existing `<timestamp>` used for the file, precise to the minute. E.g., the `<timestamp>` `2026-09-17-0231` for the file name becomes `2026-09-17T02:31Z`.
