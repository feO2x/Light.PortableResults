# AGENTS.md for AI plans

<!-- guided-coding-version: 2.0.0 -->

This directory is the append-only record of how work in this repository was planned and how it
turned out. It follows the [Guided Coding](https://kenny-codes.net/docs/guided-coding/) approach.

Plan filenames use `YYYY-MM-DD-HHMM-<issue-id>-<kebab-case-description>.md`, timestamped in UTC
when written. Omit the issue segment when work has no tracker issue. Plan Deviations filenames use
`YYYY-MM-DD-HHMM-<issue-id>-plan-deviations.md`, or the corresponding topic when there is no issue.
Do not rename historical documents solely to conform to the current format.

Plans are frozen when their Planning Phase ends. From then on, the only permitted edit is checking
an acceptance criterion (`- [ ]` to `- [x]`) after the implementation and relevant feedback loops
verify it. Never reword, add, remove, or reorder criteria in a frozen plan. Plan Deviations
documents are frozen once committed. Never rename or delete a plan or Plan Deviations document.

Every plan starts with `# Title` followed by exactly `## Rationale`, `## Acceptance Criteria`, and
`## Technical Details`, in that order. Acceptance criteria are observable, verifiable outcomes
written as unchecked Markdown tasks. Technical Details record important decisions, constraints,
affected components, and non-obvious interactions without prescribing routine implementation.

Use a follow-up plan to correct or supersede a frozen plan. Always write a Plan Deviations document
when at least one follow-up plan exists, summarizing which earlier decisions they superseded and the final
outcome. Also use a Plan Deviations document when the final implementation materially changes or
rejects an explicit plan decision. Plan Deviations must identify the compared plans and describe
what was planned, what changed, why it changed, the final implementation, and the impact.

Before writing a plan or Plan Deviations document, use the corresponding Guided Coding skill.
