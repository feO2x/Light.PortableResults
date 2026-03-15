# Error Definitions

## Rationale

The recent error-template redesign improved message generation, but assertion methods on `Check<T>` still lack a clean way to reuse complete validation rule definitions that include more than formatted text. Built-in and custom assertions need a model that keeps message rendering reusable, captures rule-level defaults such as code and metadata once, and allows parameterized rules such as numeric boundaries or ranges to cache immutable definitions across validation runs. This plan introduces dedicated validation error definitions on top of the existing message-template concept and uses them to implement a first set of built-in assertions for DTO-centric validation flows.

## Acceptance Criteria

- [x] Validation error infrastructure distinguishes between reusable message templates and immutable validation error definitions so that rule identity can be cached independently from message formatting logic.
- [x] Non-parameterized and parameterized validation error definitions are supported through public APIs that remain low-allocation and avoid unnecessary boxing for value-type check values and rule parameters.
- [x] Validation infrastructure exposes a public static catalog for fixed built-in definitions and a singleton cache for dynamically parameterized validation error definitions, and the current validation pipeline can access that cache through `ValidationState` and `ValidationContext`.
- [x] `Check<T>` gains ergonomic overloads for adding errors from validation error definitions, including explicit override hooks for callers who want to replace defaults such as code, metadata, target, or category.
- [x] The built-in assertion model preserves the existing short-circuit behavior so that later assertions are skipped after failures such as `IsNotNull`, preventing null-driven follow-up failures in chained checks.
- [x] Built-in `Check<T>` assertion methods expose a `shortCircuitOnError` parameter with explicit defaults: `true` for `IsNotNull` and `false` for `IsIn`, `IsGreaterThan`, and `IsLessThan`.
- [x] The validation package exposes reusable built-in definitions for at least `NotNull`, `IsIn`, `GreaterThan`, and `LessThan`.
- [x] The following assertion methods are implemented on `Check<T>` and use the new error-definition infrastructure: `IsNotNull`, `IsIn`, `IsGreaterThan`, and `IsLessThan`.
- [x] Automated tests cover the new definition, catalog, and cache model, built-in assertion behavior, target composition, default and overridden error details, cache reuse semantics, and representative custom-definition scenarios.

## Technical Details

Introduce a dedicated validation error-definition layer instead of expanding `IValidationErrorMessageTemplate` into a catch-all abstraction. Message templates should remain responsible only for producing `ValidationErrorMessage` from readonly validation context data. Validation error definitions should represent one concrete validation rule instance with defaults such as:

- error code
- error metadata
- error category, with `ErrorCategory.Validation` as the default for built-in definitions
- the message-template slot or lookup identity to use
- any parameter values that are fixed for that rule instance

Use separate public definition shapes for parameterless and parameterized rules, mirroring the current template model. The exact naming is up to the implementation, but the API should make it obvious that callers usually work with definitions while templates are the lower-level formatting primitive. The common path should therefore look like `check.AddError(definition)` rather than forcing every assertion to manually call `ProvideMessage(...)` and then assemble the final `Error`.

Cached definitions should be immutable reference types. This keeps cache reuse simple and predictable, avoids accidental copying semantics, and makes the public model easier to reason about than cached struct-based definitions hidden behind interfaces.

Cached definitions should remain independent from per-run template instances. A definition may identify which built-in template slot it wants to use, but it should resolve the actual message template through the active `ValidationContext` when the message is created. This ensures that one cached rule definition can still respect per-run customization of built-in templates through `ValidationContextOptions`.

Keep `ValidationErrorMessageContext<T>` and `ReadOnlyValidationContext` in place. Definitions should not mutate the validation context and should not receive `Check<T>` directly. `Check<T>` remains the owner of target normalization, short-circuit handling, and final `Error` materialization. When a definition is added to a check, `Check<T>` should:

- normalize the target if necessary
- create the readonly message context
- ask the definition or its underlying template for the message
- combine the generated message with the definition defaults
- apply optional caller overrides for code, metadata, target, or category
- add the final `Error` to the `ValidationContext`

Expose explicit overrides on the `Check<T>` APIs rather than hard-coding validation-package behavior that callers cannot change. The default path should still produce validation errors with sensible defaults, but callers must be able to replace the category or other details intentionally when they need that flexibility.

The plan should also preserve and document the existing short-circuit model for assertion chains. This is not just an optimization; it is a correctness requirement for fluent validation code. Assertions such as `IsNotNull` must be able to fail and short-circuit subsequent assertions so that later checks do not dereference or compare `null` values and throw follow-up exceptions while the validation pipeline is still supposed to accumulate errors safely.

Each built-in `Check<T>` assertion method introduced by this plan should expose a `bool shortCircuitOnError` parameter. The default should be `true` for `IsNotNull`, because it acts as the null guard in a fluent assertion chain, and `false` for `IsIn`, `IsGreaterThan`, and `IsLessThan`, so callers opt into additional short-circuiting behavior explicitly.

Add singleton-safe definition infrastructure with a clear split between fixed built-ins and dynamic reuse. The default model should use both of the following:

- a public static catalog that exposes shared definition instances for built-in rules whose semantics are fixed, such as `NotNull`
- a singleton cache for dynamically parameterized rule definitions such as ranges and comparison thresholds

The singleton cache should be injected into `ValidationState`. The cache implementation itself is shared across validation runs, but `ValidationContext` should expose it publicly for ergonomic access from built-in or custom validation code. This gives extension methods a simple access path through `ValidationContext` without making the cache itself per-run state. Do not store the primary cache on `ValidationContext` itself because its lifetime is only one validation run.

The cache contract should be public so that callers can benefit from it in advanced scenarios, but the design should still avoid baking every implementation detail into the surface area. Prefer a public cache abstraction plus a default public implementation over an internal-only helper or a purely concrete hidden cache type. The singleton cache must be thread-safe because it is shared across validation runs.

The singleton cache should mainly serve parameterized reusable rule instances such as range and comparison rules. For parameterized rules like `IsIn`, `IsGreaterThan`, and `IsLessThan`, the cache should reuse immutable definitions by their effective rule identity so that repeated use of the same boundaries or comparison values does not recreate identical definition instances across validation runs. Cache keys must capture the semantics of the rule, including the fixed parameter values and any defaults that influence the produced `Error`. Fixed built-ins should not pay this lookup cost when a static shared definition instance is sufficient. Per-call overrides such as explicit target, metadata override, category override, or other `AddError(...)` call-site details must not become part of the cache key because they are not part of reusable rule identity.

Keep `ValidationErrorTemplates` focused on per-run message-template customization. It should remain the place where callers can replace built-in message rendering behavior, localization behavior, or formatting logic for validation messages. The separate validation error-definition infrastructure introduced by this plan should not collapse those message-template concerns back into the configuration object.

The validation package should expose:

- a public static catalog of fixed built-in definitions for rules whose semantics never change, such as `NotNull`
- the lower-level template types that those definitions can resolve through the active `ValidationContext`
- the dynamic cache for parameterized rule definitions whose semantics depend on user-supplied boundary values or
  thresholds

The `NotNull` definition in the built-in catalog should replace the current direct use of the `NotNull` message-template slot at assertion call sites. Comparable and range rules should reuse shared built-in message-template slots through the active validation context, but their concrete rule instances should come from the dynamic cache when parameter values are part of the rule identity.

Provide the first built-in assertions as usage examples of the new infrastructure:

- The extension methods for `Check<T>` introduced by this plan should be placed in the
  `Light.PortableResults.Validation.Assertions` namespace and the corresponding `Assertions` folder of the validation
  project.
- `IsNotNull` should use the built-in `NotNull` definition and stop manually assembling errors at the call site.
- `IsGreaterThan` should create or reuse a definition for the comparative value and produce a failure when the checked
  value is `null` or not greater than the comparative value according to the current validation semantics.
- `IsLessThan` should mirror the greater-than rule for the opposite boundary.
- `IsIn` should support a lower and upper boundary and create or reuse a cached range definition. The message-generation
  behavior should follow the current Light.Validation-style “between lower and upper boundary” semantics rather than
  inventing a completely new wording model.

The plan should make null-handling semantics explicit for the new assertions. The intended model for this package should be that `IsNotNull` is the assertion that turns `null` into a validation failure, while `IsGreaterThan`, `IsLessThan`, and `IsIn` all skip `null` values. Combined with short-circuiting after `IsNotNull`, this keeps fluent assertion chains predictable and prevents follow-up checks from treating null-handling inconsistently. If the implementation deliberately chooses different semantics, that contract must be documented and covered by tests rather than remaining implicit.

For range and comparison definitions, metadata should be designed so that callers can attach machine-readable boundary information once and reuse it. The defaults can be static metadata for fixed rules or metadata factories that are evaluated when the definition is created, but the resulting definition instance should remain immutable and reusable. This is the main scenario where definitions are more valuable than bare templates: one shared message template can back many cached rule definitions with different codes and boundary metadata.

Automated tests should be written through the public validation API. In addition to the normal success and failure paths for the four built-in assertions, add tests that verify:

- custom definitions can be implemented without touching internal infrastructure
- `Check<T>.AddError(definition, ...)` uses definition defaults when no overrides are provided
- explicit overrides replace the definition defaults
- cached comparable and range definitions are reused for equivalent rule parameters
- built-in assertions produce the expected target paths in parent, child, and indexed validation scopes
- metadata and codes for boundary-based rules are preserved in the final `Error`
- one cached definition can be reused across validation runs with different template customizations while still using
  the active run's templates for message generation
- short-circuited checks stop later assertions after a failure such as `IsNotNull`, preventing unsafe chained access to
  `null` values
- the built-in assertion methods honor the documented `shortCircuitOnError` defaults and optional overrides
