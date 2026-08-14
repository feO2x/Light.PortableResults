---
description: Review a completed Guided Coding plan draft against the codebase before its Planning Phase ends. Run only when explicitly requested by the user.
license: MIT
metadata:
    github-path: skills/guided-coding-review-plan
    github-ref: refs/heads/main
    github-repo: https://github.com/feO2x/guided-coding
    github-tree-sha: f02d971824425244e54bb006335f8b2885cb7b6b
name: guided-coding-review-plan
---
# Review a Plan

Review the plan named by the user. If none is named, proceed only when exactly one uncommitted plan
draft exists in `ai-plans/`; otherwise ask for its path. Report findings only; never edit the plan
or any other file.

Read the plan, applicable repository instructions, the code it discusses, and every earlier
document for the same issue, including legacy filenames. Verify assertions about existing types,
members, files, APIs, and extension points. Do not reject files or types merely because the plan
intends to create them.

Amongst other things, check for:

- **Acceptance criteria:** Each criterion describes an observable, verifiable outcome rather
  than an implementation step. Behavior changes require appropriate automated test coverage.
- **Verifiability:** The required feedback loops exist in repository instructions, build files,
  scripts, or CI configuration.
- **Technical decisions:** Data structures, abstractions, and framework mechanisms fit the
  surrounding architecture without unnecessary indirection.
- **Level of detail:** The plan records non-obvious decisions and interactions without
  prescribing routine implementation.
- **Omissions:** Judge relevant risks such as error handling, compatibility, migration,
  performance, security boundaries, and observability against the change at hand.
- **History:** A follow-up plan explicitly identifies and explains any earlier decisions it
  supersedes.

Say explicitly when there are no findings. If the plan is already frozen, note that corrections
require a follow-up plan rather than edits.
