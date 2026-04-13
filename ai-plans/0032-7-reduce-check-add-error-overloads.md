# Reduce Check<T>.AddError(...) overloads

## Rationale

`Check<T>` currently exposes six public `AddError(...)` overloads that span three different abstraction levels: fully materialized `Error` instances, generated messages, and reusable validation definitions/templates. This makes one of the central validation entry points look larger and more infrastructure-heavy than the rest of the package. The recent validation plans already established that reusable `ValidationErrorDefinition` instances should be the primary public model, while message templates are a lower-level formatting primitive. This plan reduces the public `Check<T>.AddError(...)` surface to a smaller, more coherent set of overloads without hiding capabilities behind internal APIs.

## Acceptance Criteria

- [x] `Check<T>` exposes only three public `AddError(...)` overload families: one for `Error`, one for `ValidationErrorDefinition`, and one for plain string messages.
- [x] The public `Check<T>.AddError(ValidationErrorMessage, ...)` overload is removed.
- [x] The public `Check<T>.AddError(IValidationErrorMessageTemplate, ...)` overload is removed.
- [x] The public generic `Check<T>.AddError<TParameter>(IValidationErrorMessageTemplate<TParameter>, TParameter, ...)` overload is removed.
- [x] The remaining string-based `AddError(...)` overload has the exact intended signature, including explicit `ErrorCategory` support for imperative custom-validation scenarios.
- [x] Existing built-in assertions and internal validation infrastructure continue to work after the public overload reduction, using `ValidationErrorDefinition` as the reusable rule abstraction.
- [x] Public tests that exist only to exercise the removed template-based `Check<T>.AddError(...)` overloads are deleted, and replacement tests validate the remaining public API, including template-backed definitions as the migration path for callers who previously passed templates directly.
- [x] The removed message- and template-based `Check<T>.AddError(...)` overloads are not reintroduced through new helper methods or extension methods that recreate the same public surface elsewhere.
- [x] README and XML documentation are updated so examples and API guidance reflect the reduced overload set and the intended distinction between raw errors, simple imperative messages, and reusable definitions.

## Technical Details

Reduce the `Check<T>.AddError(...)` API to the following public surface:

- `AddError(Error error, bool respectShortCircuit = true)`
- `AddError(ValidationErrorDefinition definition, ...)`
- `AddError(string message, ...)`

The remaining string-based overload should use this signature:

```csharp
public Check<T> AddError(
    string message,
    string? code = null,
    MetadataObject? metadata = null,
    ValidationTarget? target = null,
    ErrorCategory category = ErrorCategory.Validation,
    bool respectShortCircuit = true
)
```

The `ValidationErrorDefinition` overload should remain the primary reusable-rule entry point. This aligns with the earlier validation design work where definitions carry stable rule identity, default code, metadata, optional target, and optional category, while templates are only responsible for message generation. Callers who currently want to reuse a template directly should instead wrap it in `TemplateValidationErrorDefinition` or `TemplateValidationErrorDefinition<TParameter>`.

Remove the public `ValidationErrorMessage` and template-based `AddError(...)` overloads from `Check<T>`. They expose lower-level messaging infrastructure directly on the main fluent validation type and are largely redundant:

- a caller with a fully materialized domain or protocol error can use `AddError(Error, ...)`
- a caller with a reusable validation rule should use `AddError(ValidationErrorDefinition, ...)`
- a caller doing imperative ad-hoc validation can use `AddError(string, ...)`

To avoid regressing imperative scenarios, the remaining string-based overload should explicitly support `ErrorCategory` in addition to the existing message, code, metadata, target, and short-circuit controls. The goal is that custom validation delegates and lightweight item-validation lambdas do not have to construct `Error` objects just to emit a non-default category.

Do not reduce `ValidationContext.AddError(...)` as part of this plan unless implementation work shows a closely related cleanup that is necessary for consistency. The main focus is the fluent `Check<T>` API because it is the overload-heavy entry point that users encounter during validator authoring.

Update internal and test code to use the reduced model consistently:

- built-in assertions should continue to route through `ValidationErrorDefinition`
- tests in `ValidationConfigurationTests.cs` and `ValidationErrorMessageCachingTests.cs` whose purpose is to exercise the removed `Check<T>.AddError(IValidationErrorMessageTemplate, ...)` and `Check<T>.AddError<TParameter>(IValidationErrorMessageTemplate<TParameter>, TParameter, ...)` overloads should be deleted rather than migrated one-to-one
- do not keep a separate category of tests for direct template-based `Check<T>.AddError(...)` usage after the overload reduction; any remaining template-related behavior that still matters must only be covered indirectly through `ValidationErrorDefinition`, especially `TemplateValidationErrorDefinition` and `TemplateValidationErrorDefinition<TParameter>`
- no tests should remain for `Check<T>.AddError(ValidationErrorMessage, ...)` after the overload is removed; if `ValidationErrorMessage` still requires dedicated coverage, that coverage should live with lower-level messaging types rather than with the `Check<T>` API

The replacement tests should cover the supported `Check<T>.AddError(...)` surface directly:

- `AddError(Error, ...)` preserves explicit target information and still fills in the current check target when the supplied error has no target
- `AddError(ValidationErrorDefinition, ...)` remains the reusable-rule path, including template-backed definitions, override behavior, and message-cache behavior
- `AddError(string, ...)` covers imperative custom-validation scenarios, including explicit category selection, target overrides, and short-circuit handling

Documentation should clearly describe the intended decision model:

- use `AddError(string, ...)` for one-off imperative failures
- use `AddError(ValidationErrorDefinition, ...)` for reusable validation rules and custom messages/templates
- use `AddError(Error, ...)` only when the caller intentionally wants to provide the fully materialized final error

Because the library is still pre-stable, this breaking API simplification is acceptable. The plan should still treat migration clarity as important: examples and tests should demonstrate the replacement of removed template overloads with `TemplateValidationErrorDefinition` so that the new surface feels smaller without feeling less capable.
