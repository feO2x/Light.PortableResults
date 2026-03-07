# Validation Support Foundations

## Rationale

Light.PortableResults already models validation failures well at the transport boundary through
`ErrorCategory.Validation`, `Error.Target`, and HTTP validation problem serialization, but it does not yet provide an
in-process validation API for DTOs. This plan introduces the foundational validation types by selectively adapting the
successful parts of Light.Validation: a `ValidationContext` that owns configuration and error creation, a low-allocation
`Check<T>` struct for validating a single value, a dedicated `ValidationOutcome<T>` result model, and validator base
classes for DTO-centric workflows. The design should align with PortableResults primitives instead of copying the old
library verbatim: validation failures become flat `Error` entries inside `Errors`, hierarchical paths are represented
through composed `Error.Target` values such as `address.zipCode` or `addresses[0].zipCode`, synchronous convenience
APIs can still produce plain `Result` failures for Minimal APIs or MVC, and validators may either normalize a DTO in
place or transform it into another validated type for anti-corruption boundaries.

## Acceptance Criteria

- [ ] A new project `src/Light.PortableResults.Validation` exists, targets `netstandard2.0`, references
  `Light.PortableResults`, is added to the solution, and `src/AGENTS.md` is updated to describe the new project.
- [ ] A corresponding test project `tests/Light.PortableResults.Validation.Tests` exists for the new validation package
  and is added to the solution.
- [ ] The validation foundation exposes public types for `ValidationContext`, `Check<T>`, `ValidationOutcome<T>`,
  `BaseValidator<TSource>`, `Validator<T>`, `Validator<TSource, TValidated>`, `AsyncValidator<T>`,
  `AsyncValidator<TSource, TValidated>`, `IValidationContextFactory`, a default `ValidationContextFactory`, and the
  supporting options/template types needed to create contexts.
- [ ] `ValidationContext` lazily accumulates validation failures as flat `Error` entries with
  `ErrorCategory.Validation`, creates `Check<T>` instances via caller argument expressions, normalizes strings when
  configured, and materializes failures as `Errors` / `Result` values without nested error containers.
- [ ] `Check<T>` is implemented as a low-overhead readonly struct that supports normalized value flow,
  target/display-name handling, short-circuiting, and manual error creation so that future check extension methods can
  build on it without redesigning the type.
- [ ] `ValidationOutcome<T>` preserves the validated value on success, stores flat `Errors` on failure, and provides
  helpers to convert failures into `Result` so that endpoint code can stay concise without losing a path to normalized
  values.
- [ ] `Validator<T>` performs automatic null checking, delegates rule execution to
  `PerformValidation(ValidationContext, T)`, and returns `ValidationOutcome<T>` while also offering synchronous
  convenience APIs such as `CheckForErrors(...)` / `TryValidate(...)` for short endpoint code.
- [ ] `Validator<TSource, TValidated>` supports validation plus transformation to a different validated output type,
  enabling immutable records, commands, and anti-corruption-layer mappings.
- [ ] `AsyncValidator<T>` and `AsyncValidator<TSource, TValidated>` provide the same validation and transformation
  model for I/O-bound validation workflows and return `Task<ValidationOutcome<T>>` /
  `Task<ValidationOutcome<TValidated>>`.
- [ ] Nested validation is modeled through target prefixing rather than nested `Errors` instances, and the foundation
  provides the path-composition hooks needed for future child-validator and collection-validation extensions.
- [ ] Automated tests cover successful validation, failure accumulation, target normalization, string normalization,
  automatic null validation, and flat hierarchical target composition.

## Technical Details

Create `Light.PortableResults.Validation` as a framework-agnostic companion package, not as an ASP.NET-specific
integration. It should target `netstandard2.0` like the core project so the validation API can be reused in HTTP, gRPC,
messaging, and non-web code. Because the design relies on `CallerArgumentExpressionAttribute` for ergonomics, the
project will need the same kind of compatibility support that the solution already uses for newer C# features on older
target frameworks.

For the original design reference, review these Light.Validation sources before implementing the new types:

- `ValidationContext`: <https://github.com/feO2x/Light.Validation/blob/main/Code/Light.Validation/ValidationContext.cs>
- `Check<T>`: <https://github.com/feO2x/Light.Validation/blob/main/Code/Light.Validation/Check.cs>
- `Validator<T>`: <https://github.com/feO2x/Light.Validation/blob/main/Code/Light.Validation/Validator.cs>
- `BaseValidator<T>`: <https://github.com/feO2x/Light.Validation/blob/main/Code/Light.Validation/BaseValidator.cs>
- `AsyncValidator<T>`: <https://github.com/feO2x/Light.Validation/blob/main/Code/Light.Validation/AsyncValidator.cs>
-
`IValidationContextFactory`: <https://github.com/feO2x/Light.Validation/blob/main/Code/Light.Validation/IValidationContextFactory.cs>
-
`ValidationContextOptions`: <https://github.com/feO2x/Light.Validation/blob/main/Code/Light.Validation/ValidationContextOptions.cs>
- `ValidationResult<T>`: <https://github.com/feO2x/Light.Validation/blob/main/Code/Light.Validation/ValidationResult.cs>
- `ErrorTemplates`: <https://github.com/feO2x/Light.Validation/blob/main/Code/Light.Validation/Tools/ErrorTemplates.cs>
- README examples: <https://github.com/feO2x/Light.Validation/blob/main/README.md>

Do not port Light.Validation’s `ExtensibleObject`, `Dictionary<string, object>` error store, or
`MultipleErrorsPerKeyBehavior`. Those concepts were useful for nested error graphs, but they conflict with
PortableResults’ flat `Errors` value object. `ValidationContext` should instead own a lightweight internal error builder
that avoids allocations on the success path and grows only when errors are added. The builder can stay internal, but it
should be designed like the rest of the library: keep the first error inline, expand lazily, and only materialize an
`Errors` instance when the caller asks for one. `ValidationContext` should also own immutable references to
`ValidationContextOptions` and `ValidationErrorTemplates`, expose `Check<T>(...)`, `AddError(...)`, `TryGetErrors(...)`,
and convenience methods that convert the current state into `Errors` and `Result` failures.

`ValidationContextOptions` should keep only the options that still make sense with flat errors: target normalization,
string normalization, and automatic-null-error creation. The old key comparer and multiple-errors-per-key settings
should be dropped. Likewise, the target normalizer must be redesigned for PortableResults. The default behavior should
preserve member paths instead of taking only the last segment: `dto.Address.ZipCode` should become `address.zipCode`,
and collection/indexer syntax should remain expressible as `addresses[0].zipCode`. Keep the normalizer pluggable through
options because expression-text cleanup is inherently heuristic. An empty target string must continue to represent the
root object because the existing HTTP serialization already treats `""` as the root validation target.

`ValidationErrorTemplates` should stay close to Light.Validation’s strengths, but adapted to `Error`. Keep the localized
format strings and formatting helpers so future rule extensions can reuse them, but do not make templates responsible
for storing errors. The future check extension methods should use the templates to create `Error.Message` values while
setting `Error.Code`, `Error.Target`, `Error.Category`, and optional `MetadataObject` explicitly. This is where the
PortableResults metadata system comes in: machine-readable validation details belong on `Error.Metadata`, not in a
mutable `object` bag on the context.

`Check<T>` should mirror the successful shape of Light.Validation’s `Check<T>` while fitting the current library style.
Make it a readonly struct that carries the context, raw or normalized target, display name, value, and a short-circuit
flag. Keep the implicit conversion to `T` so normalized values can be reassigned naturally in validators. Add manual
primitives that future extension methods can compose around: methods such as `WithValue`, `WithDisplayName`,
`ShortCircuit`, `NormalizeTargetIfNecessary`, and `AddError`. `AddError` should support both fully constructed `Error`
values and a simpler overload for message-based failures, automatically applying the check’s target when no explicit
target is provided.

Introduce a dedicated `ValidationOutcome<T>` value type as the primary result model of the validation package. It
should expose the validated value on success, flat `Errors`, and `IsValid` / `HasErrors` semantics, plus helpers such
as `TryGetErrors(...)` and `ToFailureResult()` so callers can easily convert failures into `Result` for HTTP and MVC
integration. The type should not revive Light.Validation’s loosely typed `ValidationResult<T>` shape; it should stay
aligned with PortableResults primitives by using `Errors` directly.

Introduce a public `BaseValidator<TSource>` plus synchronous and asynchronous validator base classes. `Validator<T>`
should keep Light.Validation’s ergonomic `PerformValidation(ValidationContext, T)` shape for the common case where
validation returns the same DTO type after normalization. In addition, add `Validator<TSource, TValidated>` with
`PerformValidation(ValidationContext, TSource)` returning `TValidated` so validation can also form an anti-corruption
layer that maps incoming DTOs to immutable record structs, commands, or other validated internal models. Mirror both
shapes with `AsyncValidator<T>` and `AsyncValidator<TSource, TValidated>` using asynchronous
`PerformValidationAsync(...)`
methods and returning `Task<ValidationOutcome<T>>` / `Task<ValidationOutcome<TValidated>>`.

The public API should center on `Validate(...)` overloads returning `ValidationOutcome<T>` or
`ValidationOutcome<TValidated>`, with overloads that accept an existing `ValidationContext` for multi-step validation
pipelines. On top of that, provide synchronous convenience methods tailored to endpoint ergonomics, for example
`CheckForErrors(...)` and `TryValidate(...)`, so callers can keep short patterns such as
`if (validator.CheckForErrors(dto, out Result failure)) return failure.ToMinimalApiResult();`. These convenience
methods should be thin adapters over `ValidationOutcome<T>` rather than the primary abstraction, because `out`-based
patterns do not translate to async validators. Async validators should therefore expose only outcome-based APIs such as
`ValidateAsync(...)`, keeping the model consistent while avoiding awkward `out`-parameter designs.

For transformed validators (`TSource -> TValidated`), the validated output should only be considered available on
success. That keeps the API clean for immutable and anti-corruption use cases where a partially built output object is
usually not meaningful. For same-type validators (`T -> T`), callers can still mutate a reference-type DTO in place
while validating, but the formal contract remains `ValidationOutcome<T>` rather than exposing partially normalized
values on failure through `Result<T>`.

To preserve a path for future child validators without nested error trees, keep a scoped-context concept in the design.
`IValidationContextFactory` should support creating both root contexts and child/scoped contexts that share the same
underlying error sink while prepending a target prefix. The child context no longer owns its own nested error
dictionary; instead, it contributes flat errors whose targets are composed from the parent prefix and the child member
path. That keeps future `ValidateWith(...)` and `ValidateItems(...)` extensions straightforward and compatible with the
existing HTTP validation serialization logic.

Tests in `Light.PortableResults.Validation.Tests` should stay mostly unit-level and focus on behavior that locks down
the architectural decisions in this plan: no allocations or state buildup on the success path beyond the context
instance, string normalization semantics, target normalization for nested members, automatic null failures with root
targets, conversion of accumulated failures into `Errors`, `Result`, and `ValidationOutcome<T>`, synchronous
endpoint-oriented convenience methods, transformed validation from `TSource` to `TValidated`, async validation result
flow, and scoped-context composition yielding paths such as `address.zipCode` and `addresses[0].zipCode`.
