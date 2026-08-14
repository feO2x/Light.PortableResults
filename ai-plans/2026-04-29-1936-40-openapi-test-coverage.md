# Improve OpenAPI Test Coverage

## Rationale

The OpenAPI work is currently covered by a single test project, `Light.PortableResults.AspNetCore.OpenApi.Tests`, even though the production code is split across `Light.PortableResults.AspNetCore.OpenApi` and `Light.PortableResults.Validation.OpenApi`. This makes ownership blurry, leaves public workflows in the validation bridge under-tested, and makes it too easy to miss gaps in package-specific coverage. Coverage feedback also needs to be gathered with `coverage.runsettings`; otherwise generated `obj/**` OpenAPI source-generator files distort the numbers and hide the real gaps. With `coverage.runsettings` applied, the current line-coverage baselines are approximately **88.8%** for `Light.PortableResults.AspNetCore.OpenApi` and **83.2%** for `Light.PortableResults.Validation.OpenApi`.

This plan reorganizes the tests around package boundaries and public behavior. The primary goal is to add a dedicated `Light.PortableResults.Validation.OpenApi.Tests` project, move validation-specific OpenAPI tests there, and expand both suites with sociable unit tests that exercise realistic in-memory ASP.NET Core/OpenAPI flows before adding any narrowly scoped lower-level tests.

## Acceptance Criteria

- [x] Coverage work for the OpenAPI packages is measured with `coverlet.collector` together with `coverage.runsettings`, so generated `obj/**` files do not distort the feedback loop for `Light.PortableResults.AspNetCore.OpenApi` and `Light.PortableResults.Validation.OpenApi`.
- [x] A new test project `Light.PortableResults.Validation.OpenApi.Tests` is added to the solution, references the validation OpenAPI bridge package and its required runtime collaborators, includes `coverlet.collector`, and follows the repository testing conventions.
- [x] Validation-specific OpenAPI tests are moved or rewritten so that `Light.PortableResults.AspNetCore.OpenApi.Tests` focuses on the generic OpenAPI package while `Light.PortableResults.Validation.OpenApi.Tests` owns the validation bridge package.
- [x] The validation OpenAPI test suite covers the public workflows of `BuiltInValidationErrorContracts`, `RegisterBuiltInValidationErrors`, and the typed `WithEqualToError<T>`, `WithNotEqualToError<T>`, `WithGreaterThanError<T>`, `WithGreaterThanOrEqualToError<T>`, `WithLessThanError<T>`, `WithLessThanOrEqualToError<T>`, `WithInRangeError<T>`, `WithNotInRangeError<T>`, and `WithExclusiveRangeError<T>` helpers on both `PortableProblemOpenApiBuilder` and `PortableValidationProblemOpenApiBuilder`.
- [x] The generic OpenAPI test suite gains additional coverage for the currently under-covered public behavior in `PortableErrorMetadataContractRegistry`, `PortableOpenApiBuilderUtilities`, `PortableErrorMetadataContractEqualityComparer`, and the response-builder flows that rely on them, preferring sociable tests and using focused lower-level tests only where the behavior is difficult to reach from the outside.
- [x] The reorganized suites keep their focus on sociable unit tests built around real builders, real attributes, real registries, and in-memory OpenAPI document generation, and they do not introduce mocking libraries or solitary test patterns.
- [x] Automated tests are updated and expanded as needed, and the resulting **line coverage** for both `Light.PortableResults.AspNetCore.OpenApi` and `Light.PortableResults.Validation.OpenApi` exceeds 92% when measured with `coverage.runsettings`, without padding the suite with low-value constructor-only or reflection-heavy tests.

## Technical Details

Treat this as a test-architecture refinement, not as a runtime package redesign. `Light.PortableResults.Validation.OpenApi` already exists and should remain the runtime home for built-in validation error contracts and typed validation-specific OpenAPI helpers. The missing piece is an equally clear test boundary. Add `tests/Light.PortableResults.Validation.OpenApi.Tests` as the dedicated home for validation bridge tests, wire it into the solution, and give it the same basic setup as the other .NET 10 xUnit v3 test projects in the repository.

The existing `Light.PortableResults.AspNetCore.OpenApi.Tests` project should then be narrowed to the generic package. `BuiltInValidationErrorContractsTests` and `ValidationOpenApiDocumentTransformerTests` are the obvious starting points to move or rewrite in the new project because they primarily verify behavior from `Light.PortableResults.Validation.OpenApi`. After the split, the generic test project should mostly own:

- OpenAPI document transformer behavior that belongs to the core OpenAPI package
- generic error-contract registry and duplicate-detection behavior
- generic schema naming and schema catalog behavior
- generic route-handler and attribute-driven response documentation behavior

The new validation OpenAPI test project should own the validation bridge behaviors and keep them sociable. Prefer the style already used in the current document-transformer tests: create a minimal in-memory ASP.NET Core application with the real production service registrations, configure endpoints with real `ProducesPortableProblem(...)` / `ProducesPortableValidationProblem(...)` calls, generate an OpenAPI document through `IOpenApiDocumentProvider`, and assert the produced schemas. This keeps the tests close to how consumers actually use the packages and naturally covers multiple collaborators at once.

Structure the validation bridge tests around a few public workflows instead of many tiny helper-level assertions. A good division is:

- one fixture for the built-in contract catalog and registration extension
- one fixture for typed comparison and range helpers on `PortableProblemOpenApiBuilder`
- one fixture for the same helpers on `PortableValidationProblemOpenApiBuilder`
- one or two end-to-end document-generation fixtures that mix global built-in contracts with endpoint-scoped typed narrowing

The typed-helper coverage should be matrix-based rather than copy-pasted. Reuse theory data to drive the helper name, validation error code, and expected metadata properties so the tests stay compact while still covering every helper. Cover representative type arguments such as `int` and `DateTime` to prove that the endpoint-scoped narrowing continues to flow through the standard ASP.NET Core schema generator.

For the generic OpenAPI package, prefer outside-in coverage first and only add focused lower-level tests for branches that remain uncovered after the sociable tests are in place. In practice, this means:

- add scenarios that exercise array-appending and type-appending behavior through the real response builders instead of testing `PortableOpenApiBuilderUtilities` in isolation wherever possible
- add registry scenarios that prove duplicate equivalent contracts are accepted, conflicting contracts are rejected, and sanitized-code collisions are rejected
- add small focused tests for `PortableErrorMetadataContractEqualityComparer` only if some of its branch behavior remains unreachable through the public builder/registry contract

Do not chase percentages with passive tests that only instantiate records in `BuiltInValidationErrorMetadata.cs` or reflect over API shape without protecting meaningful behavior. If some of those types remain under-covered after the sociable tests are complete, prefer one meaningful public-contract test that causes them to participate in real document generation over a set of constructor-only tests. Only keep narrowly scoped tests where the observable public contract would otherwise be hard to protect.

Use coverage as a feedback loop throughout the work, but always run it with the repository runsettings so generated files are excluded. The expected command pattern is:

```bash
dotnet test tests/Light.PortableResults.AspNetCore.OpenApi.Tests/Light.PortableResults.AspNetCore.OpenApi.Tests.csproj --settings coverage.runsettings --collect:"XPlat Code Coverage"
dotnet test tests/Light.PortableResults.Validation.OpenApi.Tests/Light.PortableResults.Validation.OpenApi.Tests.csproj --settings coverage.runsettings --collect:"XPlat Code Coverage"
```

The first goal of the coverage pass is to confirm that package-specific gaps are shrinking after the sociable tests are added and that both OpenAPI packages clear the 92% line-coverage target with generated files excluded. Only after that should additional focused tests be introduced for the remaining uncovered public branches. The finished test layout should make package ownership obvious, keep the validation bridge tests close to the bridge package, and leave both OpenAPI suites easier to extend when new OpenAPI helpers or validation contracts are added.
