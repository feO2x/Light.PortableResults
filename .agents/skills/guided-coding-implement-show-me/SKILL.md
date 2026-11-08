---
description: Instruct a user how to implement a Guided Coding Frozen Plan by presenting and explaining complete code. Run only when explicitly requested by the user.
license: MIT
metadata:
    github-path: skills/guided-coding-implement-show-me
    github-ref: refs/tags/2.0.0
    github-repo: https://github.com/feO2x/guided-coding
    github-tree-sha: 02fdec28abe8f42c84a56e609dfe00846d3ebdd7
name: guided-coding-implement-show-me
---
# Show the User How to Implement a Frozen Plan

Your goal is to teach the user how to implement a Guided Coding Frozen Plan by splitting it into teachable milestones. For each milestone, you present and explain the code one fragment at a time; the user enters it, runs the feedback loops and manual tests, and asks about whatever is unclear. This skill is intended for the Beginning stage, so always present complete fragments: the user should not have to figure out any part of the implementation.

The user makes every change to the repository, including commits. Typing the code by hand is where much of the learning happens, so encourage it over copying and pasting.

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

Present the milestones as a short roadmap without any code.

Build it layer by layer, each milestone putting one area in focus and building on the previous ones. For a backend feature, this could be the domain model, then database access, a service, and finally the endpoint. Since nothing runs end-to-end before the last layer, explain in the roadmap how the layers will connect, and remind the user where the current layer sits whenever a milestone begins. If the plan has no layers, or the preferences ask for something else, choose another split that keeps one area in focus per milestone.

Each milestone leaves a compilable codebase whose feedback loops pass and that can be committed. Its tests are part of the code you present, since they prove the milestone works; add manual tests where needed, for example, for UI changes. A single milestone is fine if that teaches the plan best.

## 4. How to Work Through a Single Milestone

Begin each milestone with a high-level description of the changes it introduces and which parts of the plan it addresses, and ask whether the user understands it.

Then present the code fragment by fragment, never all at once, so that the user builds their mental model of the codebase step by step. Verify each fragment once the user has entered it.

When all fragments are in place, explain how to run the feedback loops or manual tests. Before the user runs them, ask what they expect to happen and why. Their answer tells you whether your explanation landed.

Answer questions about the code at any point. When something does not compile or a test fails, let the user read the error first, then point out what to focus on, for example, the information an exception carries.

Once the milestone behaves as described and you verified it, let the user tick the Acceptance Criteria it fully satisfies, if any, from `- [ ]` to `- [x]`, and commit them together with the milestone. Then update the learning profile and move on.

## 5. Update the Learning Profile

Update the profile whenever something changed: when the user expresses a preference, after each milestone once the user committed it, and when the user stops early. Re-read the file right before you edit it, and commit to `main` using `git -C "$HOME/.guided-learning"` with a Conventional Commits message whose body states what you observed. Never create branches or push, and keep learning notes out of the repository you are working in.

Change preferences only as the user says. Write knowledge nodes as nested list items:

```markdown
- **<Name>** `<Stage>` — covers <thing>, <thing>
```

Knowledge that would survive a switch to another technology stack belongs to a discipline, anything else to a technology. Nest at most three levels:

- **Technologies**: `<ecosystem>` → `<technology>` → `<area>`, for example `.NET` → `EF Core` → `change tracking`. The ecosystem is the one whose package manager distributes the technology, so React belongs to `JavaScript`, which includes TypeScript. A technology outside any ecosystem, such as PostgreSQL, is a root itself.
- **Disciplines**: `<discipline>` → `<topic>` → `<subarea>`, for example `Automated testing` → `Test doubles` → `fakes`. Roots are limited to Algorithms and data structures, Software design and architecture, Automated testing, Data modeling and persistence, Security, Concurrency and distributed systems, Performance, Delivery and operations, and User interface design. Ask the user before you add another one.

After each milestone, add a node for every area it drew on that the profile does not cover yet. Reuse existing nodes, and name technologies the way their official documentation does. Add a child only when its stage differs from its parent's; otherwise, list it in the parent's optional `covers`.

Move a stage only on what you observed. Promote at most one step per plan: to Advancing when the user carried a milestone in that area without being handed the implementation, to Mastering when they shaped the design or pushed back on the plan for a reason that held up. Correct a wrong node any distance. This skill never moves a node above Beginning, because it hands over the implementation. Change the node where you saw the evidence, and a parent only when your evidence covers all of it.

## 6. Handle Plan Issues

If a plan decision is wrong or an Acceptance Criterion cannot be met as written, work out a solution or workaround and teach it like any other part of the plan; if you cannot, report it to the user. Ideally, you catch this while creating the roadmap. You decide whether a problem that needs external input interrupts or aborts the Implementing Phase. In the Guiding Phase, the reviewer decides how to proceed with your findings.

## 7. After the Last Milestone

Summarize what you and the user accomplished and point them to the Guiding Phase. Unless you faced plan issues, all Acceptance Criteria should be ticked.
