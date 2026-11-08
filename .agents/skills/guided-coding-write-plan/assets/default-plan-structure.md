# Default Guided Coding Plan Structure

Start with `# Title`, followed by exactly these sections in order:

1. `## Rationale`: briefly state the problem and overarching goal this issue is solving.
2. `## Acceptance Criteria`: list **observable, verifiable outcomes** as unticked Markdown tasks (`- [ ]`). Describe results, not implementation steps.
3. `## Technical Details`: Provide relevant information which guides the future implementer of the plan in the right direction. For example, references to existing APIs which should be extended, Design Patterns to follow, performance and threading insights, security aspects, how to design automated tests, considerations for automated benchmarks, and links to relevant docs. This list is not complete, you can add or remove elements depending on the context. Avoid step-by-step instructions and exhaustive listings. Assume the implementer is a senior software developer.

Use minimal code examples only when they clarify an important contract, such as an API signature, interface, or DTO shape. Say whether an example is exact or illustrative. Avoid method bodies, step-by-step instructions, exhaustive file lists, and routine background.

When behavior changes, require automated test coverage in the acceptance criteria. Require benchmarks only when performance is a material risk or requirement.

The root AGENTS.md and the referenced docs in it should define feedback loops (such as compilers, static code analyzers, automated tests, test coverage, mutation testing, automated benchmarks, etc.) available in the repository. Use them to make Acceptance Criteria verifiable. If there are no feedback loops listed, stop and report this to the user.

If the plan requires a missing feedback loop, make adding it explicit in the plan.
