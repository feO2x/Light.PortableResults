# Test instructions

## Test design

- Use hand-written test doubles instead of mocking frameworks such as Moq or NSubstitute.
- Keep test classes at namespace level; nested helper and test-double types are allowed.
- Use FluentAssertions instead of xUnit's `Assert` class.
- Prefer sociable unit tests through the highest practical production API. Use solitary tests only for otherwise unreachable low-level contracts, such as guard clauses.

## Mutation triage

Use the mutation-testing feedback loop documented in the root `AGENTS.md` and interpret results as follows:

1. If behavior is genuinely unconstrained, add or strengthen a contract-focused test.
2. If a mutant is equivalent or invalid, suppress it narrowly at the source with `// Stryker disable once Statement : equivalent - <reason>`. Do not use `disable all` or global `ignore-mutations` for a single site.
3. If a construct is untestable, record the limitation rather than manufacturing a test.

Never restructure production code to improve mutation score; performance takes precedence. Tests added during triage must describe public behavior and must not mention Stryker, mutant IDs, source lines, incidental ordering, call counts, or other implementation details.

Investigate every timeout. Treat compile errors and Safe Mode as mutation-coverage gaps, and verify static-initializer survivors manually before adding tests. See `mutation-testing.md` for the operating guide and known blind spots.
