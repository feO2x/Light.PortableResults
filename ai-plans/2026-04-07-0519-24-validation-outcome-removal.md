# Validated Value And Validator Results

## Rationale

The current validation design mixes two responsibilities in `ValidationOutcome<T>`: it acts as the public validation result and as the internal carrier for normalized or transformed values. That makes failing validators awkward because child validators add errors to the shared `ValidationContext`, yet transformed outputs are still constructed and exposed through the same type. This plan separates those concerns. Public validator APIs should align with the core library and return `Result<T>`, while validator implementations should use a dedicated success-only carrier named `ValidatedValue<T>` for normalized or transformed values. Validation errors continue to live exclusively in `ValidationContext`, which lets root and child validators behave the same way and avoids constructing transformed outputs on failing paths.

## Acceptance Criteria

- [x] `ValidationOutcome<T>` is renamed to `ValidatedValue<T>` and redesigned as a success-only value carrier rather than a public validation result model.
- [x] `ValidatedValue<T>` no longer exposes `Errors`, `IsValid`, `HasErrors`, or `ToFailureResult()`, and it enforces the same non-null success semantics as `Result<T>`.
- [x] `ValidatedValue<T>` remains public only because it appears in protected extensibility points on public validator base classes, and its documentation clearly positions it as a validator-implementation type rather than an application-facing result model.
- [x] All public synchronous validator entry points return `Result<T>` / `Result<TValidated>` instead of `ValidationOutcome<T>` and remain consistent for root and child validator usage.
- [x] All public asynchronous validator entry points return `ValueTask<Result<T>>` / `ValueTask<Result<TValidated>>` and mirror the synchronous API shape.
- [x] `PerformValidation(...)` and `PerformValidationAsync(...)` on validator base classes return `ValidatedValue<T>` / `ValidatedValue<TValidated>` so subclasses can produce normalized values without materializing failures.
- [x] `ValidationContext` is the single source of validation failures for root and child validators; child validators add errors to the shared context and do not return independent failure objects.
- [x] Nested validator composition uses a non-public `ValidatedValue<T>`-based execution path instead of the public `Result<T>` API so child validators do not materialize intermediate failures.
- [x] The outer validator pipeline converts `ValidationContext` plus `ValidatedValue<T>` into the final `Result<T>` exactly once, preserving current flat target semantics, automatic null validation, and nested validation behavior.
- [x] `ValidatedValue<T>` exposes explicit construction for success and no-value states so validator implementations do not rely on an implicit default-value convention.
- [x] Successful validation never produces a `null` value, and nullable validator tests and documentation are updated to match that contract.
- [x] Synchronous convenience APIs such as `CheckForErrors(...)` and `TryValidate(...)` are updated to sit on top of the new `Result<T>` contract without duplicating failure materialization.
- [x] Automated tests are updated to cover successful normalization, failing validation with no exposed value, automatic null handling, nested child validators, transformed validators, async validators, and `ValidatedValue<T>` semantics.
- [x] Benchmarks are updated to show that failing transformed validations no longer allocate normalized output objects unnecessarily, especially in `ValidationEndpointBenchmarks`.

## Technical Details

Keep `Result<T>` as the only public success/failure model returned by validators. `Validator<T>.Validate(...)`, `Validator<TSource, TValidated>.Validate(...)`, `AsyncValidator<T>.ValidateAsync(...)`, and `AsyncValidator<TSource, TValidated>.ValidateAsync(...)` should all return the corresponding `Result<T>` shape. This aligns validation with the rest of the library and gives callers one consistent contract: success means a non-null value exists, failure means errors exist.

Rename `ValidationOutcome<T>` to `ValidatedValue<T>` and narrow its responsibility. `ValidatedValue<T>` should be the protected-path carrier used by custom validator implementations, not a second public validation result model. It should represent only whether a normalized or transformed value was produced. The type should therefore keep only success-oriented members such as:

- a non-null `Value`
- a `HasValue` indicator
- `TryGetValue(...)`
- equality members if they are still useful for tests and consumers implementing custom validators

Do not let `ValidatedValue<T>` store or expose validation errors. Remove `Errors`, `IsValid`, `HasErrors`, and `ToFailureResult()`. A default or empty `ValidatedValue<T>` means "no value was produced." Creating a successful `ValidatedValue<T>` with `null` must throw so the contract stays aligned with `Result<T>`.

Make `ValidatedValue<T>` construction explicit. Prefer a small factory surface such as `ValidatedValue.Success(value)` and `ValidatedValue.NoValue` so validator implementations communicate intent directly and do not depend on callers understanding that `default` means "no validated value."

`ValidatedValue<T>` will likely need to remain `public` because it appears in `protected` members on public validator base classes. Treat that as an implementation-driven visibility requirement, not as a sign that it should remain a primary public result abstraction. Its XML documentation should explicitly state that application code should use `Validate(...)` / `ValidateAsync(...)` and their `Result<T>` return values, while `ValidatedValue<T>` exists for custom validator authors overriding the protected pipeline methods.

Keep `ValidationContext` as the only error channel. Validators should continue to add failures through `context.Check(...)`, `context.AddError(...)`, and child contexts such as `context.For(...)`, `context.ForMember(...)`, and `context.ForIndex(...)`. This rule applies equally to root and child validators. A child validator should never need to materialize a failed `Result<T>` or failed `ValidatedValue<T>` just because it added validation errors to the shared context.

To preserve that invariant, introduce a non-public execution path for validator-to-validator composition. Public callers should continue to use `Validate(...)` / `ValidateAsync(...)` and receive `Result<T>`, but parent validators composing child validators should be able to invoke a protected or internal helper that returns `ValidatedValue<T>` while sharing the same `ValidationContext`. This avoids materializing `Result.Fail(context.ToErrors())` for intermediate child failures that the parent validator only needs to observe as "no validated value was produced."

Change the protected validator contract to use `ValidatedValue<T>`:

- `Validator<T>.PerformValidation(...)` returns `ValidatedValue<T>`
- `Validator<TSource, TValidated>.PerformValidation(...)` returns `ValidatedValue<TValidated>`
- `AsyncValidator<T>.PerformValidationAsync(...)` returns `ValueTask<ValidatedValue<T>>`
- `AsyncValidator<TSource, TValidated>.PerformValidationAsync(...)` returns `ValueTask<ValidatedValue<TValidated>>`

This lets subclasses keep transformation logic inside the validator while only constructing the final DTO, command, or normalized object on the success path. For same-type validators, returning a successful `ValidatedValue<T>` should usually wrap the normalized source object after all checks have passed. For transformed validators, the usual implementation pattern should be:

1. normalize and validate fields
2. invoke child validators through the non-public `ValidatedValue<T>` composition path when validator code is already running inside a shared `ValidationContext`
3. if the shared context now contains errors, return an empty `ValidatedValue<T>`
4. otherwise construct the transformed output and return a successful `ValidatedValue<T>`

The outer `Validate(...)` and `ValidateAsync(...)` methods should remain responsible for argument validation, automatic null handling, and final materialization. Their rule should be simple and deterministic:

- if automatic null validation fails, return `Result.Fail(...)`
- otherwise call `PerformValidation(...)`
- if the shared `ValidationContext` contains errors after `PerformValidation(...)`, return `Result.Fail(context.ToErrors())`
- if the context is clean and `ValidatedValue<T>` contains a value, return `Result.Ok(value)`
- if the context is clean but `ValidatedValue<T>` is empty, throw an `InvalidOperationException` because the validator implementation violated its contract

That last guard is important. Once errors are owned exclusively by `ValidationContext`, an empty `ValidatedValue<T>` is only valid when errors are already present. This catches broken custom validators early instead of silently returning a malformed success or failure result.

Update `BaseValidator<TSource>` so the automatic null helper returns failed `Result<T>` instances directly. The helper should continue to build errors through `ValidationContext` so target handling, display names, message templates, and shared context-item behavior remain unchanged.

Review the convenience APIs after the main pipeline is updated. `TryValidate(...)` should validate once, call `Result<T>.TryGetValue(...)`, and project failures to the non-generic `Result` only when needed. `CheckForErrors(...)` should continue to support concise endpoint code but must not trigger any duplicate error materialization or hidden transformed-output construction.

Update tests in `tests/Light.PortableResults.Validation.Tests` around the new split of responsibilities. In particular:

- tests that previously asserted `ValidationOutcome<T>` should now assert `Result<T>` from public validator APIs
- tests for nullable-success behavior must be inverted so success with `null` is no longer allowed
- transformed-validator tests should assert that values are only readable on success
- child-validator tests should confirm that nested validation still contributes flat errors to the shared context without separate failure objects
- new tests should cover `ValidatedValue<T>` directly so the success-only semantics are locked down

Update `benchmarks/Benchmarks/ValidationEndpointBenchmarks.cs` to use the new API shape and to stop constructing transformed outputs after validation has already failed. The benchmark validators should return empty `ValidatedValue<T>` instances when the shared context contains errors and only allocate `SimpleCommand`, `ComplexCommand`, `AddressCommand`, and `ItemCommand` on the success path. The benchmark acceptance check should focus on the original problem statement: failing validations must no longer pay for unnecessary transformed-object allocations.

Add at least one benchmark or focused benchmark assertion around nested validator composition so this redesign also proves that child validators do not materialize intermediate `Result<T>` or `Errors` objects before the final public boundary is reached.
