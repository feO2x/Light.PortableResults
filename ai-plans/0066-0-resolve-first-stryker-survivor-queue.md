# Resolve the First Stryker Survivor Queue

## Rationale

The first mutation-testing triage pass covered the two smallest scoped runs, `Light.PortableResults.Validation.OpenApi` and `Light.PortableResults.AspNetCore.Mvc`, and produced twenty survivors. Twelve of them are one gap — the `target`-provided branch of the typed validation OpenAPI helpers is never exercised — and that branch calls `WithErrorExample` with a `null` message, which is the symptom described in issue #57. Testing it now would encode the current behavior as the expected contract, so it is deferred and resolved together with that bug.

The remaining eight are independent and are the subject of this plan. Two are false survivors that expose a blind spot the tooling documentation does not yet describe, three are not killable and need justified suppression, and three are genuine guard-clause gaps of exactly the kind `tests/AGENTS.md` already asks for solitary tests. Resolving them leaves a survivor queue whose every remaining entry has a recorded reason, which is the state that makes future runs comparable.

## Acceptance Criteria

- [x] `tests/AGENTS.md` documents mutants in static initializers and static constructors as a blind spot, with the measured evidence and the reason the reused test host cannot kill them, so a future reader does not re-triage the same two mutants.
- [x] The two unreachable switch arms in `RegisterBuiltInValidationErrors` and the redundant `builder` guard in `ProducesPortableValidationProblemFor` carry narrow `Stryker disable once` comments with justifications; no global `ignore-mutations` setting is used.
- [x] Tests cover the null-builder guards of both typed validation OpenAPI helper families and the null-context guard of `BaseLightActionResult.ExecuteResultAsync`, asserting `ArgumentNullException`.
- [x] A re-run of both projects reports no survivor other than the twelve deferred to #57 and the two known-false static-initializer mutants; `AspNetCore.Mvc` reports zero survivors.
- [x] The baseline table in `tests/AGENTS.md` is replaced for both projects with full count vectors measured at the resulting commit, keeping the provenance the existing table records.
- [x] `dotnet test Light.PortableResults.slnx` passes, and production code changes are limited to suppression comments — no behavior changes, no CI changes, no mutation score thresholds.

## Technical Details

### Suppressions

Three mutants cannot be killed by any test that respects the contract, so they are suppressed at the source with the reason recorded in the report:

- `BuiltInValidationErrorContractRegistrationExtensions.cs`, the `ErrorMetadataTypeContract` arm — `BuiltInValidationErrorContracts.Contracts` is this method's only data source and contains schema and no-metadata contracts exclusively. The arm stays: it is correct for a registry that later gains a type contract, and removing it to satisfy the tool would be the restructuring `tests/AGENTS.md` forbids.
- The same method's `default:` arm message — `ErrorMetadataContract` declares a `private protected` constructor and has exactly three sealed subclasses, so no fourth kind can exist and the arm is unreachable by construction.
- `PortableValidationOpenApiRouteHandlerBuilderExtensions.cs`, the `ArgumentNullException.ThrowIfNull(builder)` guard — the delegated `ProducesPortableValidationProblem` guards the same parameter, so the observable contract is identical with or without it. The existing `ProducesPortableValidationProblemFor_ShouldRejectNullBuilder` test passes either way, which is why it did not kill the mutant.

Match the mutator to the suppression (`Statement` for the two statement mutants, `String` for the message) rather than disabling all mutators at the site.

### Guard clause tests

The `EnsureBuilder` overloads in `BuiltInValidationErrorBuilderExtensions` are reached by all thirty-six public typed helpers, so one test per builder family is sufficient: one helper on `PortableProblemOpenApiBuilder` and one on `PortableValidationProblemOpenApiBuilder`, each invoked on a null builder. Without the guard these produce `NullReferenceException` instead of the documented `ArgumentNullException`, which is what makes the mutants killable. `ValidationOpenApiExtensionGuardTests` is the established home for these.

`BaseLightActionResult.ExecuteResultAsync` needs the equivalent for a null `ActionContext`, reached through the sealed `LightActionResult`. The MVC test project currently has no unit-level test class; the integration tests exercise the app end to end and cannot reach this guard.

### Blind spot: static initializers

Applying the two false survivors to the source by hand fails the suite — emptying `BuiltInValidationErrorContracts.Contracts` fails 46 of 112 tests in `Light.PortableResults.Validation.OpenApi.Tests`, and blanking the built-in schema id string fails 52 of 112 — while Stryker reports both as survived. Both mutants sit in code reachable only during static initialization of a static property, which a reused test host runs once per process, before or independently of mutant activation.

Document this next to the existing Safe Mode entry, in the same shape: what the tool reports, why, and what a reader must do instead. The consequence for triage is that a survivor in a static initializer, static constructor, or a helper called only from one is verified by applying it by hand and running the affected test project, not by reading the report.

### Re-measurement

Both projects are re-run at the end to produce the recorded baseline; together they take roughly four minutes. The suppressed mutants move from `Survived` to `Ignored`, which changes the `Ignored` count that `tests/AGENTS.md` currently explains as block-removal filtering alone — that explanation needs to account for user suppressions once they exist.
