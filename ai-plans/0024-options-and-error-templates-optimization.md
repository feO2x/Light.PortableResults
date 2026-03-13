# Options And Error Templates Optimization

## Rationale

The current validation configuration model mixes mutable shared state, loosely coupled boolean-plus-delegate switches, and string-format-based error templates that are simple but not well aligned with the validation package's performance goals. At the same time, upcoming requirements around localization, translation keys, richer context-aware message generation, and root-to-child propagation of arbitrary validation data need a clearer and more cohesive design. This plan consolidates validation configuration into immutable records, replaces ambiguous flag/delegate combinations with explicit policy abstractions, and introduces allocation-aware readonly struct contexts for error-message generation so that extensibility improves without regressing the hot validation path.

## Acceptance Criteria

- [x]
  `ValidationContextOptions` is redesigned as an immutable record that represents the complete per-run validation configuration.
- [x]
  `ValidationErrorTemplates` is redesigned as an immutable record and becomes part of
  `ValidationContextOptions` instead of being passed separately through the validation pipeline.
- [x] The mutable shared singleton pattern for
  `ValidationContextOptions.Default` and
  `ValidationErrorTemplates.Default` is removed so that default instances are safe to reuse without accidental global mutation. There should be default instances for those types, but they must not be mutable.
- [x] The current
  `NormalizeStringValues` and
  `NormalizeStringValue` members are replaced by a single
  `IStringValueNormalizer` abstraction with a single normalization method and at least one no-op implementation.
- [x] The current
  `CreateAutomaticNullErrors` and
  `CreateAutomaticNullError` members are replaced by a single
  `IAutomaticNullErrorProvider` abstraction with a
  `TryCreateError(...)` method.
- [x] Validation runs can expose arbitrary shared context items to parent and child scopes through mutable APIs on
  `ValidationContext`, while readonly access to those items remains available during error-message generation and automatic-null handling.
- [x] A dedicated readonly struct
  `ReadOnlyValidationContext` is introduced for readonly access to validation-run configuration and shared context data without boxing the mutable
  `ValidationContext` struct.
- [x] Error-message generation uses readonly struct message contexts and generic APIs so that value-type inputs are not boxed by default when templates inspect the validated value.
- [x]
  `ValidationErrorTemplates` stores message-template abstractions rather than raw format strings, enabling constant messages, specialized formatting, localization-aware formatting, and machine-readable error keys without forcing every message through
  `string.Format(params object?[])`.
- [x] The new message-template design supports both plain message text and localization-oriented scenarios such as hierarchical frontend translation keys.
- [x] The public validation API remains ergonomic for custom validators, while the common built-in validation path avoids avoidable allocations from interface boxing,
  `object` boxing, and params-array formatting.
- [x] Automated tests cover immutable default instances, string normalization policies, automatic-null policies, mutable shared context-item access across parent and child scopes, readonly context-item access during message generation, generic message-template behavior for reference and value types, localization-oriented message generation, and preservation of current validation semantics.
- [x] Benchmarks are added or updated to measure the redesigned configuration and message-generation hot paths, including at least built-in string normalization, automatic-null handling, constant message generation, formatted message generation, and message generation for value-type checks.

## Technical Details

Redesign
`ValidationContextOptions` as an immutable record that becomes the single configuration root for a validation run. Instead of threading
`ValidationContextOptions` and
`ValidationErrorTemplates` separately through
`ValidationContextFactory`,
`ValidationState`, and the validators, store the templates inside the options object. This aligns the design with the current shared-state model: one root validation run should have one coherent configuration object that is safe to share across all child scopes.

`ValidationErrorTemplates` should also become an immutable record. Do not keep the current mutable shared-singleton behavior where
`Default` can be changed globally at runtime. The default instances for both options and templates must be safe to share. If customization is needed, callers should create new instances via record construction or
`with` expressions instead of mutating a shared default.

Replace the current boolean-plus-delegate configuration pairs with dedicated policy abstractions:

-
`IStringValueNormalizer` with a single method for string normalization
-
`IAutomaticNullErrorProvider` with a single
`TryCreateError(...)` method

The string normalizer should not require a separate enable/disable boolean. A no-op implementation should represent the "do nothing" case. The method should allow preservation of
`null` values so that disabling normalization does not implicitly convert nulls into empty strings. Keep the built-in implementations sealed and reuse singleton instances for the common policies.

Likewise, automatic null-error behavior should be modeled through
`IAutomaticNullErrorProvider` instead of a boolean plus optional delegate. The provider decides whether an automatic null error should be created. A no-op provider disables this feature without needing a separate switch. The built-in default provider should create the same validation errors as today unless the caller supplies an alternate implementation.

Introduce a dedicated readonly struct
`ReadOnlyValidationContext`. Do not use an interface such as
`IReadOnlyValidationContext`, because the validation context is now a struct and interface-based readonly access would box it.
`ReadOnlyValidationContext` should provide readonly access to the shared validation-run state needed by message templates and policies, such as:

- the current target prefix
- the active
  `ValidationContextOptions`
- the active
  `ValidationErrorTemplates`
- readonly lookup of arbitrary validation context items
- any additional future run-level readonly data needed for message generation

The writable
`ValidationContext` should expose a cheap conversion or view-creation mechanism to produce a
`ReadOnlyValidationContext` backed by the same shared
`ValidationState`.

Support arbitrary shared context items that can be written through
`ValidationContext` and are visible from parent and child scopes alike. These items should live in shared run-level state so that nested scopes can read them without per-scope allocation and parent validators can compute values for child validators to consume later in the same validation run. The writable API belongs on
`ValidationContext`, while
`ReadOnlyValidationContext` should expose only readonly lookup. The API should be designed carefully to avoid string-key collisions and untyped casts where possible; typed keys are preferable. The plan does not require mutable scope-local shadowing semantics or rollback behavior when leaving a child scope. A single shared per-run item store is sufficient and is easier to reason about.

Redesign
`ValidationErrorTemplates` so that it stores message-template abstractions rather than raw strings. The existing
`string.Format(..., params object?[])` model is flexible but allocates more than necessary for the common built-in cases and does not provide a natural place for richer message metadata. The intended shape of the boxing-aware API should be close to the following:

```csharp
public interface IValidationErrorMessageTemplate
{
    string ProvideMessage<T>(in ValidationErrorMessageContext<T> context);
}

public readonly struct ValidationErrorMessageContext<T>
{
    public ReadOnlyValidationContext Context { get; }
    public string DisplayName { get; }
    public string Target { get; }
    public T Value { get; }
}

public readonly struct ReadOnlyValidationContext
{
    public string TargetPrefix { get; }
    public ValidationContextOptions Options { get; }
    public ValidationErrorTemplates ErrorTemplates { get; }

    public bool TryGetItem<T>(ValidationContextKey<T> key, out T value);
}
```

Avoid
`object? Value` because that would box value types. Avoid interface-based context access because that would box the validation context struct. These are explicit performance goals of the redesign. The readonly validation context inside these message contexts should still provide access to the shared context-item store so that callers can base message generation on data attached earlier in the validation run.

Do not couple message-template generation to
`Check<T>` directly. While
`Check<T>` is a convenient source of data in many validation paths, automatic null handling and future validation scenarios must also be able to generate messages without depending on that exact type. A dedicated readonly struct message context is the better contract.

The new message-template design should support more than just raw human-readable text. It must be possible to implement:

- constant messages such as "The value must not be null"
- specialized display-name-based messages without going through
  `string.Format`
- localization/globalization-aware messages that use culture or other run-level state from the readonly validation context
- machine-readable keys such as
  `errors.notnull` that can be consumed by a frontend or translation layer

The exact carrier type for message generation can be either a string-only abstraction or a richer message descriptor, but the design must support localization-oriented and translation-key-oriented workflows without pushing callers back to ad hoc string concatenation. Keep the common built-in templates optimized via sealed singleton implementations where possible.

Update the built-in validation code paths to use the new configuration model. This includes
`ValidationContextFactory`,
`ValidationState`,
`ValidationContext`,
`Check<T>`,
`BaseValidator<TSource>`,
`Validator<T>`,
`Validator<TSource, TValidated>`,
`AsyncValidator<T>`, and
`AsyncValidator<TSource, TValidated>`. The automatic null-check path in particular should be refactored to call into
`IAutomaticNullErrorProvider.TryCreateError(...)` using readonly struct contexts rather than the current delegate-based approach.

Preserve existing validation behavior unless the new configuration explicitly changes it. In particular:

- current flat target semantics must remain unchanged
- current normalization behavior should remain the default behavior through the default
  `IStringValueNormalizer`
- current automatic null-validation behavior should remain the default behavior through the default
  `IAutomaticNullErrorProvider`
- current default built-in error messages should remain available, even if the underlying implementation moves from strings to message-template objects

Test this redesign via the public API of the Validation project. The automated tests should confirm not only correctness but also the intended configuration semantics:

- immutable defaults are not globally mutable
- custom options flow from root to child scopes
- context items written by a parent validator are visible in child scopes and message templates
- no-op and custom normalizers behave correctly
- no-op and custom automatic-null providers behave correctly
- generic message contexts work for both reference-type and value-type values without changing behavior
- localization-aware or key-based message templates can access the readonly validation context as intended

Finally, add focused microbenchmarks for the new policy and message-generation infrastructure so the cost of the redesign remains measurable. In addition to rerunning relevant endpoint validation benchmarks, add benchmarks around:

- built-in string normalization through the new normalizer abstraction
- built-in automatic null handling through the new provider abstraction
- constant message-template generation
- formatted or display-name-based message-template generation
- message-template generation for value-type checks to confirm the design avoids avoidable boxing

These benchmarks should validate that the API becomes more expressive without sacrificing the low-allocation goals of the validation package.
