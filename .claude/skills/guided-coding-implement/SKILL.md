---
description: Implement a Guided Coding Frozen Plan independently and verify the implementation through the repository's feedback loops. Run only when explicitly requested by the user.
license: MIT
metadata:
    github-path: skills/guided-coding-implement
    github-ref: refs/tags/2.0.0
    github-repo: https://github.com/feO2x/guided-coding
    github-tree-sha: 33abff53503a84ae5959617239d51e0430e29607
name: guided-coding-implement
---
# Implement a Frozen Plan

Your goal is to implement a Frozen Plan created in the Planning Phase of Guided Coding. Once you are finished implementing, a reviewer will check your results in the Guiding Phase.

## 1. Establish the Target

Use the plan named by the user. If none is named, proceed only when there is exactly one plan in `ai-plans/` that has all its Acceptance Criteria unchecked, and it has the latest timestamp of all plans. Otherwise, ask for its path.

Verify that the plan is frozen: its file name has a timestamp, and it has a `*Frozen at ...*` line below its title. If either marker is missing, explain that the Planning Phase is unfinished and stop.

## 2. Implement and Verify

Implement the plan, use the feedback loops to verify your code changes.

The only allowed plan edit is ticking an Acceptance Criterion from `- [ ]` to `- [x]`. Never check a criterion unless it is genuinely satisfied by a feedback loop. The reviewer would otherwise have to identify the gap later in the Guiding Phase, which is one of the hardest errors to spot.

## 3. Handle Plan Issues

If a plan decision is wrong or an Acceptance Criterion cannot be met as written, try to solve it or find a workaround, and report it. If a problem genuinely cannot be solved, that's totally fine - simply report it.

In the Guiding Phase, the reviewer can decide how to proceed with your findings.

## 4. Finish

Report what was implemented, which feedback loops ran and their results, which Acceptance Criteria are checked, and which remain unchecked and why. Do not create commits, open or update a pull request, publish anything, etc. - unless the user asked you to do so.
