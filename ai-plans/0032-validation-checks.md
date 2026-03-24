# Validation Checks Expansion

## Rationale

The validation package now has the structural pieces that make fluent DTO validation viable: scoped `ValidationContext` instances, explicit target semantics, reusable validation error definitions, and a first set of `Check<T>` assertions. What is still missing is breadth. Callers can build validators today, but they still have to drop down to manual `AddError(...)` calls for many common scenarios that older Light.Validation versions and FluentValidation cover out of the box.

This plan expands the built-in `Check<T>` assertion catalog in a way that fits Light.PortableResults.Validation instead of copying older libraries mechanically. The new surface should cover the most common null/default, equality, comparison, range, string, collection, enum, and decimal-validation scenarios while preserving the package's existing performance goals: explicit target ownership, low allocations on the success path, reusable immutable definitions, and no reflection-heavy or expression-tree-driven API design. The result should feel broad enough for day-to-day DTO validation, close the biggest gaps to FluentValidation, and still remain coherent enough that future assertions can be added without redesigning the core model again.

## Acceptance Criteria

- [ ] The validation package exposes a broad first-party assertion catalog in `Light.PortableResults.Validation.Assertions` that covers these required families: equality and null/default-state assertions, comparable/range assertions, string assertions, collection-count assertions, enum assertions, decimal precision/scale assertions, and a predicate-based escape hatch for custom rules.
- [ ] The first implementation wave includes exactly the following assertions: `IsNull`, `IsEqualTo`, `IsNotEqualTo`, `IsEmpty`, `IsNotEmpty`, `IsGreaterThanOrEqualTo`, `IsLessThanOrEqualTo`, `IsNotIn`, `IsInExclusiveRange`, `IsNotNullOrWhiteSpace`, `HasMinLength`, `HasMaxLength`, `HasLengthIn`, `Matches`, `IsEmail`, `ContainsOnlyDigits`, `ContainsOnlyLettersAndDigits`, `HasCount`, `HasMinCount`, `HasMaxCount`, `IsInEnum`, `IsEnumName`, `HasPrecisionAndScale`, and `Must`.
- [ ] The package exposes a separate imperative custom-validation hook in addition to `Must`, so callers can add arbitrary errors without overloading the semantics of assertion-style APIs.
- [ ] The implementation explicitly preserves and documents the current fluent-validation semantics for short-circuiting, null-guard expectations, string-normalization interaction, and target composition in nested and indexed validation scopes.
- [ ] Assertion methods that encounter `null` on non-short-circuited checks throw `InvalidOperationException` with a clear message that points callers to automatic null checking, string normalization, or an explicit `IsNotNull` guard, while short-circuited checks still return immediately without throwing.
- [ ] Cross-property assertion overloads are intentionally out of scope for this implementation wave; the listed assertions validate the current check value against constants, options, or reusable rule descriptors, not against sibling-property expressions.
- [ ] `ValidationErrorTemplates`, built-in validation error definitions, metadata keys, and the shared definition cache are expanded so that the new assertions reuse immutable rule definitions instead of recreating equivalent rule data on every validation run.
- [ ] The public API guidance documents the intended semantics and naming of the built-in assertions so the surface stays broad without becoming inconsistent.
- [ ] Automated tests cover representative success and failure paths, null/default semantics, short-circuiting, target propagation, metadata generation, and cache reuse for each assertion family.

## Technical Details

Use the following existing references to decide the breadth of the new assertion surface and the places where we should intentionally diverge:

- Previous Light.Validation checks:
- <https://github.com/feO2x/Light.Validation/blob/main/Code/Light.Validation/Checks/Checks.Common.cs>
- <https://github.com/feO2x/Light.Validation/blob/main/Code/Light.Validation/Checks/Checks.Comparable.cs>
- <https://github.com/feO2x/Light.Validation/blob/main/Code/Light.Validation/Checks/Checks.Numeric.cs>
- <https://github.com/feO2x/Light.Validation/blob/main/Code/Light.Validation/Checks/Checks.Strings.cs>
- <https://github.com/feO2x/Light.Validation/blob/main/Code/Light.Validation/Checks/Checks.Collections.cs>
- FluentValidation built-ins:
- <https://docs.fluentvalidation.net/en/latest/built-in-validators.html>

The old Light.Validation sources are useful for remembering which checks existed, but the new package should not copy its overload explosion mechanically. The current validation package already has reusable definitions, templates, and `Check<T>.AddError(...)` overloads for custom behavior. The built-in checks should therefore optimize for a coherent, low-allocation core surface instead of reproducing every old `string? message` and `Func<..., string>` overload. Callers who need custom messages or categories can already supply custom definitions or use `AddError(...)` directly. This plan should add breadth in terms of semantics, not breadth in terms of overload count.

Keep the static partial `Checks` type and split implementation files by assertion family, for example:

- `Checks.Equality.cs`
- `Checks.Comparable.cs`
- `Checks.Strings.cs`
- `Checks.Collections.cs`
- `Checks.Enums.cs`
- `Checks.Decimals.cs`
- `Checks.Predicate.cs`

The required assertion families should be implemented as follows:

- Equality and null/default-state assertions:
- Add `IsNull<T>` as the explicit inverse of `IsNotNull<T>`.
- Add `IsEqualTo<T>` and `IsNotEqualTo<T>`. These should support `EqualityComparer<T>.Default` by default and must additionally expose `IEqualityComparer<T>` overloads for non-default equality semantics.
- Add `IsEmpty` / `IsNotEmpty`, but do not try to force one reflection-heavy generic implementation over every type. Prefer explicit overload families whose semantics are clear. For this implementation wave, the supported shapes should be defined explicitly:
- `Check<Guid>`: `Guid.Empty`
- collection shapes that are handled through shared count logic: zero vs non-zero count
- `Check<string>` / `Check<string?>`: `IsEmpty` means `null` or `string.Empty`, `IsNotEmpty` means neither `null` nor `string.Empty`, and whitespace is not treated as empty
- other defaultable value types: out of scope for this plan
- `IsNotEmpty(Guid)` from Light.Validation should become part of this broader family rather than remaining a one-off method.
- For strings, `IsNotEmpty` and `IsNotNullOrWhiteSpace` should be documented as intentionally different guards: `IsNotEmpty` requires at least one character, while `IsNotNullOrWhiteSpace` requires non-whitespace content.

- Comparable and range assertions:
- Add `IsGreaterThanOrEqualTo<T>` and `IsLessThanOrEqualTo<T>`.
- Add `IsNotIn<T>` as the inverse of the current inclusive `IsIn<T>`.
- Add an explicit exclusive-range assertion, named `IsInExclusiveRange<T>` or an equivalently clear name. Do not overload `IsIn` with a boolean parameter that changes inclusive vs exclusive semantics.
- Keep using the current comparison model based on `Comparer<T>.Default` so the package stays usable with the same kinds of comparable types as the existing `IsGreaterThan`, `IsLessThan`, and `IsIn` implementations.
- Comparison and range assertions should no longer skip `null` values. If such an assertion encounters `null` on a non-short-circuited check, it must throw with a clear message that callers either need automatic null checks enabled or must guard explicitly with `IsNotNull`. Short-circuited checks must still return immediately without throwing.

- String assertions:
- Add `IsNotNullOrWhiteSpace`.
- Add length assertions with inclusive semantics as the canonical API: `HasMinLength`, `HasMaxLength`, and `HasLengthIn`.
- Add `Matches`. Prefer this name over `IsMatching` because it is the clearer verb and aligns well with the rest of the assertion surface.
- `Matches` should support both a `Regex` overload and a pattern-based overload that includes `RegexOptions`. Cache the definition data by pattern plus options, not by `Regex` object identity.
- Add `IsEmail`.
- Add `ContainsOnlyDigits` and `ContainsOnlyLettersAndDigits`.
- The old `Normalize` helper is not an assertion and does not belong to this plan. String normalization remains the responsibility of `ValidationContext` and optional value normalizers.
- Null semantics for string assertions should be explicit:
- `IsNotNullOrWhiteSpace` owns the null-or-empty-or-whitespace failure case for strings
- `Matches`, `HasMinLength`, `HasMaxLength`, `HasLengthIn`, `IsEmail`, `ContainsOnlyDigits`, and `ContainsOnlyLettersAndDigits` must throw when they encounter `null` on a non-short-circuited check, with a clear message that callers either need automatic null checks and string normalization enabled or must guard explicitly with `IsNotNull`. Short-circuited checks must still return immediately without throwing.

- Collection assertions:
- Add `HasCount`, `HasMinCount`, and `HasMaxCount`.
- Add collection-specific `IsEmpty` and `IsNotEmpty` overloads for shapes with cheap count access.
- Add dedicated `ImmutableArray<T>` overloads for the count-based assertions so this struct-based collection can use `Length` directly without boxing, interface dispatch, or any fallback path that would allocate.
- Count assertions should support `IEnumerable`-based inputs, including `ImmutableArray<T>`, but they should still optimize for cheap-count shapes first. The shared count logic should:
- use `string.Length` when the runtime value is `string`
- use `ImmutableArray<T>.Length` for `ImmutableArray<T>`
- use `Count` for `ICollection`, `ICollection<T>`, and `IReadOnlyCollection<T>`
- fall back to enumeration only when no cheap-count shape is available
- `IsEmpty` / `IsNotEmpty` for collections should build on the same shared count logic as `HasCount`, `HasMinCount`, and `HasMaxCount`.
- `HasCount`, `HasMinCount`, and `HasMaxCount` should support strings via `Length`, while `IsEmpty` / `IsNotEmpty` should support strings through their explicit string-specific semantics from the equality and null/default-state assertion family rather than by treating strings as generic collections.
- If a collection assertion encounters `null` on a non-short-circuited check, it must throw with a clear message that callers either need automatic null checks enabled or must guard explicitly with `IsNotNull`. Use `InvalidOperationException` for this case. Short-circuited checks must still return immediately without throwing.
- `ValidateItems` already exists and is not replaced by these count assertions. Collection-size checks and item validation should stay as complementary features.

- Enum assertions:
- Add `IsInEnum<TEnum>` for enum values whose numeric backing value may be invalid.
- Add `IsEnumName<TEnum>` for strings that should match enum member names, with a case-sensitivity option.
- The old numeric `TryParseToEnum` helper is useful, but it is a parsing API rather than an assertion API. It should not be folded into this plan's assertion scope.
- `IsEnumName<TEnum>` must throw when it encounters `null` on a non-short-circuited check, with a clear message that callers either need automatic null checks and string normalization enabled or must guard explicitly with `IsNotNull`. Short-circuited checks must still return immediately without throwing.

- Decimal assertions:
- Add `HasPrecisionAndScale` for `decimal`, including an `ignoreTrailingZeros` option, mirroring the semantics that FluentValidation popularized.
- Keep this focused on validation only. The old numeric `Round` methods are value-normalization helpers and should stay out of scope for this assertion plan.
- `HasPrecisionAndScale` must throw when it encounters `null` on a non-short-circuited check, with a clear message that callers either need automatic null checks enabled or must guard explicitly with `IsNotNull`. Short-circuited checks must still return immediately without throwing.

- Predicate-based escape hatch:
- Add `Must` as the general-purpose built-in for checks that do not justify a dedicated first-party assertion yet.
- `Must` should remain an assertion-style API. It should evaluate a predicate and, on failure, add one validation error that still fits the definition/template architecture.
- `Must` should therefore prefer overloads that accept a predicate plus a reusable `ValidationErrorDefinition`, a template, or a simple built-in predicate-failure definition instead of forcing callers back into ad-hoc string factories.
- A context-aware predicate overload such as `Func<ReadOnlyValidationContext, T, bool>` is reasonable when callers need access to shared run-level data while still producing a single assertion failure.
- `Must` must return immediately and must not execute user-provided predicates when the check is already short-circuited.
- Do not treat delegate identity as reusable rule identity. Predicates are execution behavior, not cache keys. The reusable piece is the error definition, not the delegate.
- Add a separate imperative custom-validation hook for scenarios where callers need to add arbitrary errors directly to the current validation run. Do not overload `Must` with this responsibility. The exact method name can be `Custom`, and its shape should follow `check.Custom((context, value) => ...)` so callers can work directly with the current value plus the active validation context. It must communicate that the delegate may add zero, one, or many errors and is not just one assertion with one failure identity.
- `Custom` must return immediately and must not execute the user-provided delegate when the check is already short-circuited.
- The intended distinction should be documented with examples such as:

```csharp
check.Must(value => IsValid(value), definition);
```

```csharp
check.Custom((context, value) =>
{
    if (...)
        context.AddError(...);

    if (...)
        context.AddError(...);
});
```

Expand the built-in definition and template infrastructure together with the assertion methods. The new checks should not manually assemble `Error` values inline when a reusable definition makes the rule identity cacheable and keeps the API customizable. Specifically:

- Add template slots and built-in definitions for the new rule families, for example: `Null`, `Empty`, `NotEmpty`, `EqualTo`, `NotEqualTo`, `GreaterThanOrEqualTo`, `LessThanOrEqualTo`, `NotIn`, `ExclusiveRange`, `Count`, `MinCount`, `MaxCount`, `Enum`, `EnumName`, `Predicate`, and `PrecisionScale`.
- Reuse the current pattern of static shared definitions for parameterless rules and cached immutable definitions for parameterized rules.
- For more complex parameter sets, prefer small dedicated value objects or readonly structs over `object[]` style parameter bags. Examples include:
- a length constraint descriptor
- a count constraint descriptor
- a regex pattern descriptor
- a precision/scale descriptor
- Add machine-readable metadata only for stable rule semantics such as boundaries, expected counts, regex pattern, expected precision, expected scale, enum type, or case-sensitivity. Do not store transient implementation details in metadata just because the old library created a message string from them.

Document the naming strategy so the surface does not drift into a mix of overlapping method names:

- Keep currently established names such as `IsNotNull`, `IsGreaterThan`, `IsLessThan`, and `IsIn`.
- Prefer names that describe the semantic directly instead of old Light.Validation phrasing where the old phrasing is less clear today:
- `HasMinLength` / `HasMaxLength` over `IsLongerThan` / `IsShorterThan`
- `Matches` over `IsMatching`
- an explicit exclusive-range name over a boolean option on `IsIn`

The null/default semantics of the new assertions must be spelled out in XML docs and tests. At minimum:

- guard assertions such as `IsNotNull`, `IsNull`, `IsEmpty`, and `IsNotEmpty` determine whether null/default values are failures
- follow-up assertions such as comparisons, pattern checks, length checks, enum-name checks, collection-count checks, and precision/scale checks should throw when they encounter null values on non-short-circuited checks instead of silently skipping them
- `Check<string?>` normalization must be called out in the documentation for string-oriented assertions so callers understand when `null` has already become `string.Empty`
- `IsEmpty` / `IsNotEmpty` must document their supported type-specific semantics explicitly instead of relying on one vague “default value” description
- the string contracts of `IsEmpty`, `IsNotEmpty`, `HasCount`, and `IsNotNullOrWhiteSpace` must be documented side by side so callers can choose the correct guard deliberately

The old libraries also surface validators that are reasonable but should stay outside the required first implementation wave. Capture them as discussion candidates so they are visible without inflating the immediate scope:

- `IsCreditCard`
- `StartsWith`, `EndsWith`, and `Contains` with explicit `StringComparison`
- `DoesNotMatch`
- `HasCountIn`
- sign-oriented numeric sugar such as `IsPositive`, `IsNegative`, `IsZero`, and `IsNonNegative`

Automated tests should stay public-API-driven. In addition to straightforward happy-path and failure-path coverage, add tests that lock down:

- short-circuit behavior for guard assertions and follow-up assertions
- root, member, and indexed target propagation for each family
- definition-cache reuse for parameterized assertions
- the chosen throwing behavior when follow-up assertions encounter null values
- string-normalization interaction for `IsNotNullOrWhiteSpace`, `Matches`, and length checks
- string-specific behavior of `IsEmpty`, `IsNotEmpty`, and `HasCount`
- collection-count behavior on `IEnumerable`, cheap-count collection shapes, `string`, and `ImmutableArray<T>`, including the dedicated allocation-free `ImmutableArray<T>` overloads
- enum validity checks for valid and invalid numeric values and enum names
- precision/scale behavior with and without trailing-zero suppression
