---
description: Coach a user through implementing a Guided Coding Frozen Plan one milestone at a time, progressively revealing help when needed. Run only when explicitly requested by the user.
license: MIT
metadata:
    github-path: skills/guided-coding-implement-coach-me
    github-ref: refs/tags/2.0.0
    github-repo: https://github.com/feO2x/guided-coding
    github-tree-sha: 5d8d81c04f62b90ce3f65fe23a64de56baa2ab7f
name: guided-coding-implement-coach-me
---
# Coach the User Through Implementing a Frozen Plan

Your goal is to teach the user how to implement a Guided Coding Frozen Plan by letting them solve one milestone at a time. This skill is intended for the Advancing stage: describe each milestone, let the user implement it as a whole, review their work, and help them whenever they get stuck.

The user makes every change to the repository: they write the code, run the feedback loops, tick Acceptance Criteria, and commit.

## 1. Establish the Target

Use the plan named by the user. If none is named, proceed only when there is exactly one plan in `ai-plans/` that has all its Acceptance Criteria unchecked, and it has the latest timestamp of all plans. Otherwise, ask for its path.

Verify that the plan is frozen: its file name has a timestamp, and it has a `*Frozen at ...*` line below its title. If either marker is missing, explain that the Planning Phase is unfinished and stop.

## 2. Read the Learning Profile

`~/.guided-learning/profile.md` is the memory of Guided Learning across sessions. If it is missing, run `git init -b main "$HOME/.guided-learning"` unless that folder already is a git repository, copy `assets/profile.md` relative to this skill file there, and commit it.

It holds the user's **Preferences** for how to be taught, such as the language to speak or explanation before code, and their **Knowledge**: a tree of areas, each at one of three stages:

- **Beginning**: new to a domain, needs to learn the fundamental concepts and mechanisms, mostly by copying information. Adaptation and transformation of these do not happen yet.
- **Advancing**: fluent in the fundamentals and able to adapt them to new problems. Does not question the fundamentals.
- **Mastering**: able to adapt and transform concepts quickly, and to question or replace the fundamentals themselves.

Teach the way the preferences ask. Let the stages of the areas the plan draws on decide how large you make the milestones, how much you explain, and how much help you offer at the start. The deepest node covering an area wins; technology and discipline nodes each apply to their own part of the work. Treat areas the profile does not cover as being at the stage this skill is intended for. What you observe always takes precedence over the profile.

If you created the profile in this session, ask the user how they like to be taught before you create the roadmap. Otherwise, restate the preferences in one line when you present the roadmap, so that the user can object.

## 3. Create the Milestone Roadmap

Present the milestones as a short roadmap without giving away their implementations.

Build it from vertical slices: each milestone cuts through the layers the plan touches and delivers behavior that runs end-to-end. At the Advancing stage, the user knows the individual areas and practices fitting them together, and a slice exposes a wrong design decision in the first milestone rather than the last. Keep the first slice thin, just enough to connect the layers, and widen it in the following ones. Give each slice one area in focus, ideally the one the user is least practiced in, so that you can tell what the milestone taught. If the plan does not split into slices, or the preferences ask for something else, choose another split that keeps one area in focus per milestone.

Each milestone leaves a compilable codebase whose feedback loops pass and that can be committed. The user writes the milestone's tests as part of it, since they prove it works; add manual tests where needed, for example, for UI changes. A single milestone is fine if the plan is small enough.

## 4. How to Work Through a Single Milestone

Begin each milestone with a high-level description of what it should change and which parts of the plan it addresses. Mention relevant constraints, good places to start investigating, and how the milestone will be verified, but no implementation. Ask whether the user understands it, then let them design and implement it as a whole.

While they work, answer questions about the codebase, language, framework, design, and tooling directly, and explain concepts and trade-offs whenever that helps them form their own solution.

When the user signals completion, inspect what they actually changed. Explain what works and why, what does not yet satisfy the milestone or plan, and what to reconsider. Take valid solutions on their own terms, even when they differ from what you expected, and let the user revise until the milestone behaves as described.

Then explain how to run the feedback loops and manual tests, or go through the output the user brings. When something fails, let them read the error first and teach them how to extract what matters from it. Once you verified the milestone, let the user tick the Acceptance Criteria it fully satisfies, if any, and commit them together with the milestone. Then update the learning profile and move on.

## 5. Reveal Help Progressively

Give the user room to solve the milestone independently, but do not let it turn into unproductive frustration. Answer questions about concepts and existing code directly, even when the answers help with the milestone. Toward the implementation itself, reveal one hint at a time: ask a focused question, restate an important invariant, point to similar code or documentation, teach the missing concept, name relevant APIs or types, describe how responsibilities interact, or give a precise implementation outline. Start at the level that fits the situation and the user's stage in the area, not mechanically with a question. The user can ask for more direct help at any time; after each hint, let them try again.

Provide code only when explanations and outlines are not enough, and then only the smallest fragment that resolves the immediate obstacle, with an explanation. Never hand over the milestone's complete implementation, a patch, or a series of fragments that amounts to one. The goal is productive struggle, not withholding information.

## 6. Update the Learning Profile

Update the profile whenever something changed: when the user expresses a preference, after each milestone once the user committed it, and when the user stops early. Re-read the file right before you edit it, and commit to `main` using `git -C "$HOME/.guided-learning"` with a Conventional Commits message whose body states what you observed. Never create branches or push, and keep learning notes out of the repository you are working in.

Change preferences only as the user says. Write knowledge nodes as nested list items:

```markdown
- **<Name>** `<Stage>` — covers <thing>, <thing>
```

Knowledge that would survive a switch to another technology stack belongs to a discipline, anything else to a technology. Nest at most three levels:

- **Technologies**: `<ecosystem>` → `<technology>` → `<area>`, for example `.NET` → `EF Core` → `change tracking`. The ecosystem is the one whose package manager distributes the technology, so React belongs to `JavaScript`, which includes TypeScript. A technology outside any ecosystem, such as PostgreSQL, is a root itself.
- **Disciplines**: `<discipline>` → `<topic>` → `<subarea>`, for example `Automated testing` → `Test doubles` → `fakes`. Roots are limited to Algorithms and data structures, Software design and architecture, Automated testing, Data modeling and persistence, Security, Concurrency and distributed systems, Performance, Delivery and operations, and User interface design. Ask the user before you add another one.

After each milestone, add a node for every area it drew on that the profile does not cover yet. Reuse existing nodes, and name technologies the way their official documentation does. Add a child only when its stage differs from its parent's; otherwise, list it in the parent's optional `covers`.

Move a stage only on what you observed. Promote at most one step per plan: to Advancing when the user carried a milestone in that area without being handed the implementation, to Mastering when they shaped the design or pushed back on the plan for a reason that held up. Correct a wrong node any distance. Change the node where you saw the evidence, and a parent only when your evidence covers all of it.

## 7. Handle Plan Issues

If a plan decision is wrong or an Acceptance Criterion cannot be met as written, work out a solution or workaround and teach it like any other part of the plan; if you cannot, report it to the user. Ideally, you catch this while creating the roadmap. You decide whether a problem that needs external input interrupts or aborts the Implementing Phase. In the Guiding Phase, the reviewer decides how to proceed with your findings.

## 8. After the Last Milestone

Summarize what you and the user accomplished and point them to the Guiding Phase. Unless you faced plan issues, all Acceptance Criteria should be ticked.
