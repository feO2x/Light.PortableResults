---
description: Write a Guided Coding plan or follow-up plan after its approach has been discussed. Run only when explicitly requested by the user.
license: MIT
metadata:
    github-path: skills/guided-coding-write-plan
    github-ref: refs/tags/2.0.0
    github-repo: https://github.com/feO2x/guided-coding
    github-tree-sha: b4fdb943b5653407986e70ade81073e3e3d8576d
name: guided-coding-write-plan
---
# Write a Plan

Your goal is to write an Initial Plan or Follow-Up Plan into the `ai-plans/` folder at the repository root. After writing, report the plan path and stop.

## Plan File Name

The initial file name of the new plan file is `<short-title>.md`. `<short-title>` is a shortened kebab-case version of the ticket title. At this point in the conversation, you should be able to derive a full title from the discussion that you can shorten. If not, ask the user to provide one and shorten it.

## Plan Structure

Check whether `ai-plans/AGENTS.md` contains the plan structure. If not, fall back to `assets/default-plan-structure.md` relative to this skill file.

## What Happens After Writing a Plan?

The user reviews the plan. They will provide feedback, aspects of the plan might change. Once both you and the user agree on the plan being ready for implementation, it is frozen and becomes largely immutable (except for checking off Acceptance Criteria). When a plan is frozen, its file name is changed to `<timestamp>-<ticket-id>-<short-title>.md`, or `<timestamp>-<short-title>.md` (if no `<ticket-id>` is present). Also, a `*Frozen at ...*` line is inserted right below the `# Title` of the plan. All this doesn't happen in this step of the conversation.

## Follow-up Plans

A Follow-Up Plan supersedes decisions of a Frozen Plan. The decision to write one is made in the Guiding Phase, when the review of the implementation calls for larger changes.

Use the same format and file naming convention. Read every earlier document for the ticket. In the opening section of the plan (`## Rationale` in the default structure), name the plans this one follows by exact filename.

Record only the changed decisions and outcomes. State exactly which earlier decisions this plan supersedes and why. Do not repeat unchanged decisions or contradict earlier plans silently.

## Most Importantly

Try to keep each plan as concise as possible. Simpler language is preferred over elaborate prose.
