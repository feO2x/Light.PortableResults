# 0024 Plan Deviations

This document compares `ai-plans/0024-validation-support.md` with the current implementation state on this branch.
It also tracks the follow-up changes introduced through:

- `ai-plans/0024-validation-context-optimization.md`
- `ai-plans/0024-options-and-error-templates-optimization.md`
- `ai-plans/0024-validation-outcome-removal.md`
- `ai-plans/0024-normalization-optimization.md`

## Deviations From The Original Plan

### 1. Public validator APIs now return `Result<T>` instead of `ValidationOutcome<T>`
*Reference: `0024-validation-outcome-removal.md`*

**Original plan:**
The original design centered the validation package on a dedicated public result model named `ValidationOutcome<T>`.
Synchronous validators were supposed to return `ValidationOutcome<T>` / `ValidationOutcome<TValidated>`, asynchronous
validators were supposed to return `ValueTask<ValidationOutcome<T>>` / `ValueTask<ValidationOutcome<TValidated>>`,
and failing outcomes could still expose the best available validated value.

**Implemented:**
The public validator contract was aligned with the core library and now returns `Result<T>` / `Result<TValidated>` and
`ValueTask<Result<T>>` / `ValueTask<Result<TValidated>>`.

`ValidationOutcome<T>` was removed from the public API and replaced with `ValidatedValue<T>`, a success-only carrier
used on protected and internal validator execution paths. Validation errors now live exclusively in
`ValidationContext`, and failed public results no longer expose normalized or transformed values.

The endpoint-oriented convenience methods from the original plan still exist, but they now sit on top of the
`Result<T>` contract:

- `CheckForErrors(...)` materializes a non-generic `Result` only on failure.
- `TryValidate(...)` returns the validated output only on success.
- Child-validator composition uses non-public `ValidatedValue<T>` helpers so nested validation does not materialize
  intermediate failed `Result<T>` instances.

### 2. `ValidationContext` was redesigned from a per-scope object into a scoped struct over shared state
*Reference: `0024-validation-context-optimization.md`*

**Original plan:**
The original plan described `ValidationContext` as the central mutable validation object and expected
`IValidationContextFactory` to create both root and child contexts that share an error sink while composing targets.

**Implemented:**
`ValidationContext` is now a `readonly struct` that represents a scoped view over a single validation run.
A single `ValidationState` instance owns the run-level mutable state, including options, templates, shared items, and
error accumulation.

`IValidationContextFactory` was simplified to root-context creation only. Child scopes are now created directly from
`ValidationContext` through explicit APIs:

- `For(...)`
- `ForMember(...)`
- `ForIndex(...)`
- `WithPrefix(...)`

The error accumulator was folded into `ValidationState`. The implementation keeps the first error inline, allocates an
owned `Error[10]` when the second error arrives, grows that array when necessary, and wraps the used portion directly
into `Errors` via `ReadOnlyMemory<Error>` instead of copying into a second exact-sized array.

### 3. Validation configuration became immutable and policy-based
*Reference: `0024-options-and-error-templates-optimization.md`*

**Original plan:**
The original plan kept `ValidationContextOptions` and `ValidationErrorTemplates` as separate configuration concepts and
described them mainly as holders for normalization settings, automatic-null behavior, and reusable message text.

**Implemented:**
`ValidationContextOptions` and `ValidationErrorTemplates` are now immutable records with safe-to-reuse default
instances. `ValidationErrorTemplates` became part of `ValidationContextOptions`, so one validation run is configured by
one immutable options object.

The previous boolean-plus-delegate style configuration was replaced by explicit policy abstractions:

- `IStringValueNormalizer`
- `IAutomaticNullErrorProvider`

The options model also grew beyond the original plan to support localization and richer message generation:

- `CultureInfo` now participates in validation-message formatting.
- Shared run-level items can be stored through `ValidationContextKey<T>` and read from both mutable and readonly
  context views.
- `ReadOnlyValidationContext` was introduced so message generation and policy hooks can inspect run-level state
  without boxing the mutable `ValidationContext` struct.

### 4. Error templates now produce richer message descriptors instead of only formatted strings
*Reference: `0024-options-and-error-templates-optimization.md`*

**Original plan:**
The original plan envisioned `ValidationErrorTemplates` as a reusable source of localized format strings and formatting
helpers that future check extensions could use to build `Error.Message` values.

**Implemented:**
`ValidationErrorTemplates` now stores template objects (`IValidationErrorMessageTemplate` and
`IValidationErrorMessageTemplate<TParameter>`) instead of raw strings.

Those templates produce `ValidationErrorMessage`, a dedicated value type that carries:

- the final message text
- an optional machine-readable key

This goes beyond the original plan because templates can now support constant messages, specialized display-name
formatting, culture-aware parameter formatting, and frontend/localization key scenarios without routing every message
through `string.Format(params object?[])`.

### 5. Target normalization kept its public behavior but changed its internal architecture significantly
*Reference: `0024-normalization-optimization.md`*

**Original plan:**
The original plan required a cache-backed `IValidationTargetNormalizer` with configurable casing semantics and stable
path normalization behavior.

**Implemented:**
The public API and semantics stayed the same, but the cold-path implementation changed substantially.
`DefaultValidationTargetNormalizer.NormalizeCore` was rewritten as a single-pass span-based parser that:

- trims via spans instead of allocating trimmed strings
- avoids `Substring`-based segment cleanup
- uses stack-allocated output buffers for shorter paths
- falls back to rented `ArrayPool<char>` buffers for longer paths

This is an implementation deviation rather than a semantic one: the branch keeps the original normalization contract
while replacing the original allocation profile with a lower-allocation parser.

### 6. Benchmarks expanded beyond the two endpoint comparisons from the original plan
*Reference: all follow-up plans*

**Original plan:**
The original benchmark scope focused on two equivalent Minimal API endpoints compared against FluentValidation
`12.1.1`: one simple scenario and one more complex scenario.

**Implemented:**
Those endpoint benchmarks still exist, but the benchmark suite was broadened to measure the optimization work
introduced after the original plan:

- `ValidationEndpointBenchmarks.cs` still compares the simple and complex endpoint scenarios against FluentValidation.
- A nested-validation-only benchmark was added to isolate child-scope overhead.
- `ValidationContextBenchmarks.cs` measures nested scope creation, error accumulation, and error materialization for
  1, 2, 10, and more than 10 errors.
- `ValidationConfigurationBenchmarks.cs` measures string normalization, automatic null-error creation, and message
  template generation.

## Summary

The original validation-support plan is still recognizable in the implemented package: the new project exists, the
package exposes low-allocation checks, flat validation errors, public target-composition helpers, sync and async
validators, tests, and FluentValidation comparison benchmarks.

The major deviations are architectural. The branch moved away from a public `ValidationOutcome<T>` model, redesigned
validation around scoped `ValidationContext` structs plus shared `ValidationState`, made configuration immutable and
policy-driven, upgraded error templates into richer message-template objects, and added focused microbenchmarks to
prove the optimization work.
