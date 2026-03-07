# Validation Support Foundations

## Rationale

Light.PortableResults already models validation failures well at the transport boundary through
`ErrorCategory.Validation`, `Error.Target`, and HTTP validation problem serialization, but it does not yet provide an
in-process validation API for DTOs. This plan introduces the foundational validation types by selectively adapting the
successful parts of Light.Validation: a `ValidationContext` that owns configuration and error creation, a low-allocation
`Check<T>` struct for validating a single value, and validator base classes for DTO-centric workflows. The design should
align with PortableResults primitives instead of copying the old library verbatim: validation failures become flat
`Error` entries inside `Errors`, hierarchical paths are represented through composed `Error.Target` values such as
`address.zipCode` or `addresses[0].zipCode`, and validator APIs return `Result<T>` / `Result` instead of a separate
validation result model.

## Acceptance Criteria

- [ ] A new project `src/Light.PortableResults.Validation` exists, targets `netstandard2.0`, references
  `Light.PortableResults`, is added to the solution, and `src/AGENTS.md` is updated to describe the new project.
- [ ] A corresponding test project `tests/Light.PortableResults.Validation.Tests` exists for the new validation package
  and is added to the solution.
- [ ] The validation foundation exposes public types for `ValidationContext`, `Check<T>`, `BaseValidator<T>`,
  `Validator<T>`, `IValidationContextFactory`, a default `ValidationContextFactory`, and the supporting options/template
  types needed to create contexts.
- [ ] `ValidationContext` lazily accumulates validation failures as flat `Error` entries with
  `ErrorCategory.Validation`, creates `Check<T>` instances via caller argument expressions, normalizes strings when
  configured, and materializes failures as `Errors` / `Result` values without nested error containers.
- [ ] `Check<T>` is implemented as a low-overhead readonly struct that supports normalized value flow,
  target/display-name handling, short-circuiting, and manual error creation so that future check extension methods can
  build on it without redesigning the type.
- [ ] `Validator<T>` performs automatic null checking, delegates rule execution to
  `PerformValidation(ValidationContext, T)`, and returns `Result<T>` / `Result` APIs that fit the existing
  PortableResults model instead of reintroducing Light.Validation’s dictionary-based `ValidationResult<T>`.
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

Do not port Light.Validation’s `ExtensibleObject`, `Dictionary<string, object>` error store, or
`MultipleErrorsPerKeyBehavior`. Those concepts were useful for nested error graphs, but they conflict with
PortableResults’ flat `Errors` value object. `ValidationContext` should instead own a lightweight internal error builder
that avoids allocations on the success path and grows only when errors are added. The builder can stay internal, but it
should be designed like the rest of the library: keep the first error inline, expand lazily, and only materialize an
`Errors` instance when the caller asks for one. `ValidationContext` should also own immutable references to
`ValidationContextOptions` and `ValidationErrorTemplates`, expose `Check<T>(...)`, `AddError(...)`, `TryGetErrors(...)`,
and convenience methods that convert the current state into `Result` / `Result<T>` failures.

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

Introduce a public `BaseValidator<T>` plus the synchronous `Validator<T>` class now; async validation can be added later
without changing the synchronous API. `Validator<T>` should keep Light.Validation’s
`PerformValidation(ValidationContext, T)` shape so validators can normalize or replace the DTO while running checks. The
public API should center on `Validate(...)` overloads returning `Result<T>`, with overloads that accept an existing
`ValidationContext` for multi-step validation pipelines. Do not reintroduce `CheckForErrors(...)` or a separate
`ValidationResult<T>` type because `Result<T>` already covers the success/failure contract for this library. One
tradeoff should be documented in the implementation: when validation fails, `Result<T>` cannot carry the normalized
value. That is acceptable for the first iteration because it keeps the API aligned with PortableResults; callers that
need the partially normalized instance can still mutate reference-type DTOs in place during validation.

To preserve a path for future child validators without nested error trees, keep a scoped-context concept in the design.
`IValidationContextFactory` should support creating both root contexts and child/scoped contexts that share the same
underlying error sink while prepending a target prefix. The child context no longer owns its own nested error
dictionary; instead, it contributes flat errors whose targets are composed from the parent prefix and the child member
path. That keeps future `ValidateWith(...)` and `ValidateItems(...)` extensions straightforward and compatible with the
existing HTTP validation serialization logic.

Tests in `Light.PortableResults.Validation.Tests` should stay mostly unit-level and focus on behavior that locks down
the architectural decisions in this plan: no allocations or state buildup on the success path beyond the context
instance, string normalization semantics, target normalization for nested members, automatic null failures with root
targets, conversion of accumulated failures into `Errors` and `Result<T>`, and scoped-context composition yielding paths
such as `address.zipCode` and `addresses[0].zipCode`.
