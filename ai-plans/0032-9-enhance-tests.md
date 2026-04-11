# Enhance Validation Tests

## Rationale

`Light.PortableResults.Validation.Tests` already contains many fast unit tests, but the current suite does not align well with how the validation package is meant to be consumed. A substantial part of the suite exercises narrow implementation details such as reflection-based API-shape checks, private storage behavior, and highly specific caching mechanics, while important public workflows remain only partially covered. Coverage feedback from Rider and `dotnet test --collect:"XPlat Code Coverage"` shows that the largest gaps are in the public validator entry points, the assertion families in `Checks.*`, and the broader `CheckExtensions` collection and async workflows.

This plan restructures the validation test suite toward sociable unit tests in the sense described by Martin Fowler: prefer realistic validator scenarios that use the public validation APIs together with real production collaborators, and only keep lower-level tests where outside-in coverage is genuinely awkward or where a low-level contract is important in its own right. The result should be a smaller and clearer set of test concepts, less coupling to implementation details, and better coverage of the behaviors that actual consumers rely on.

## Acceptance Criteria

- [ ] `Light.PortableResults.Validation.Tests` is reorganized around public validation workflows and rule families instead of around implementation fragments, with the new structure favoring sociable unit tests that use real validators, real `ValidationContext` instances, and real built-in checks.
- [ ] The reworked suite covers the public synchronous validator APIs in `Validator<T>` and `Validator<TSource, TValidated>`, including fresh-context overloads, explicit-`ValidationTarget` overloads, context-based overloads, `TryValidate`, `CheckForErrors`, and the child-validation entry points.
- [ ] The reworked suite covers the public asynchronous validator APIs in `AsyncValidator<T>` and `AsyncValidator<TSource, TValidated>`, including explicit-`ValidationTarget` overloads, caller-expression overloads, cancellation behavior, and child-validation entry points.
- [ ] Automated tests cover representative success and failure flows for each built-in assertion family: null, empty, equality, comparable/range, strings, count, enums, decimals, and predicate/custom checks.
- [ ] Automated tests cover the currently under-tested collection-validation workflows in `CheckExtensions`, including mutable collections, arrays, `ImmutableArray<T>`, delegate-based item validation, validator-based item validation, transforming item validation, and asynchronous item-validation overloads.
- [ ] The reworked suite verifies target propagation and scope composition through realistic nested-validator and indexed-collection scenarios rather than relying primarily on narrow helper-level assertions.
- [ ] Tests that are primarily coupled to private fields, reflection-based API-shape assertions, or internal storage layouts are removed or reduced to the minimum necessary set, and any retained low-level tests have a clear public-contract justification.
- [ ] Message-cache and definition-cache tests remain in the suite only where they protect externally meaningful behavior such as stable-message reuse, culture-sensitive formatting, and correct target/message materialization on cache hits.
- [ ] The validation test project uses `coverlet.collector` as the standard feedback mechanism for the restructuring work, and the plan explicitly expects implementers to use coverage reports to decide which additional lower-level tests are still necessary after the sociable tests are in place.
- [ ] The final suite remains fast, deterministic, and free of mocking libraries, continuing to follow the repository guidance for sociable unit tests and manual test doubles only where a double is still actually needed.

## Technical Details

Restructure the validation test project around the public behaviors that callers see, not around the individual internal helpers that happen to implement those behaviors. The best current starting point is the style in `ValidatorTests.cs`: realistic DTO-like validators, real nested validators, real `ValidationContext` instances, and assertions against the final `Result`, `ValidatedValue<T>`, and `Errors`. Expand that style so it becomes the center of the suite instead of one file among many narrowly focused helper tests.

Use a small number of scenario-oriented test fixtures that own distinct parts of the public surface. The exact file names can be chosen during implementation, but the structure should follow these responsibilities:

- one validator-centric fixture for synchronous validation workflows
- one validator-centric fixture for asynchronous validation workflows
- one or more rule-family fixtures that exercise the built-in assertions through realistic validators or representative `ValidationContext.Check(...)` flows
- one collection-validation fixture for `ValidateChild`, `ValidateItems`, and `ValidateItemsAsync`
- one infrastructure-oriented fixture for target normalization, value normalization, and context-key behavior
- one focused caching/configuration fixture for the remaining message-template, localization, and shared-context behaviors that are difficult to observe elsewhere

The key design rule is that each test fixture should start with the highest-level API that can express the scenario. For example:

- prefer validating a DTO through a root validator over directly invoking several unrelated `Checks.*` methods in one omnibus test
- prefer a nested validator plus `ValidateChild` / `ValidateItems` over manual target construction when the scenario is about propagation through child scopes
- prefer asserting the emitted `Errors` and final validated output over asserting that a specific intermediate helper method was called or that a specific internal buffer shape exists

Cover the public validator wrappers deliberately because they are currently under-covered and easy to miss when tests go straight to the deepest overloads. The reworked suite should include representative tests for:

- `Validate(value)`
- `Validate(value, ValidationTarget, ...)`
- `Validate(value, context, ...)`
- `ValidateChildValue(value, context, ...)`
- `TryValidate(...)`
- `CheckForErrors(...)`
- the corresponding async overloads on both async validator base classes

For the built-in checks, stop relying on a few omnibus tests that each sample many unrelated families. Instead, create compact rule-family matrices that cover both success and failure paths, null-guard behavior, short-circuit behavior, and inline override behavior for each family. The tests do not need to exercise every overload in isolation when one scenario can drive multiple meaningful behaviors, but the coverage feedback should guide which overloads still need a direct representative test. In particular, the current coverage gaps indicate that these areas need explicit attention:

- string length and pattern overloads, including `Regex` and pattern-based `Matches` overloads
- immutable-array overloads for emptiness and count assertions
- comparison overloads with and without `ErrorOverrides`
- nullable enum and nullable decimal flows
- predicate overloads that take `ErrorOverrides` or context-aware predicates

`CheckExtensions` should receive a dedicated sociable treatment because it is a large public surface area. Cover it with realistic collection validators instead of isolated helper assertions. Include scenarios for:

- validator-based item validation over mutable lists
- validator-based item validation over `ImmutableArray<T>`
- delegate-based item validation for primitive items
- delegate-based item normalization for mutable collections
- transforming item validation for arrays, lists, and immutable arrays
- async item validation and async transforming item validation for the supported collection shapes
- cancellation during async item validation
- short-circuited collection checks and null-collection guard behavior

Reduce or remove the tests that primarily pin internal implementation structure rather than behavior. This applies especially to tests that:

- inspect private fields of `Errors` or other types to verify storage layout
- reflect over public methods merely to assert overload shape
- verify internal call counters in ways that duplicate more meaningful observable assertions

If a low-level test is retained, it should be because the behavior is difficult to observe from the outside and the contract is still worth protecting. Good examples are target normalization rules, value-normalizer semantics, `ValidationContextKey<T>` identity behavior, and a small number of caching tests that verify stable-message reuse and culture-aware formatting.

Keep the caching tests focused on observable outcomes rather than on exhaustively pinning the exact internal sequence of cache calls. It is enough to verify representative behavior such as:

- stable template-backed definitions reuse a cached message across validation runs
- unstable templates bypass caching
- culture changes produce distinct formatted messages and cache entries
- cache hits still materialize the correct target and display-name-dependent message

Use `coverlet.collector` during the restructuring work as the feedback loop for deciding which lower-level tests are still needed after the new sociable tests are in place. The implementer should run coverage repeatedly, for example with:

```bash
dotnet test tests/Light.PortableResults.Validation.Tests/Light.PortableResults.Validation.Tests.csproj --collect:"XPlat Code Coverage"
```

The coverage report should be used to find the remaining public branches that are not reached by the higher-level tests. Only after that pass should additional focused tests be added for genuinely uncovered public contracts. The intent is to let coverage identify the irreducible low-level gaps instead of starting from a helper-by-helper test design.

During the rewrite, prefer deleting superseded tests instead of preserving both the old and new structures side by side. The finished project should read as one coherent suite with clear ownership boundaries, not as an accumulation of historical layers. The resulting tests should remain straightforward for future contributors to extend when new validation assertions or validator flows are added.
