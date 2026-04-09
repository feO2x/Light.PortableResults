# Failure Overrides for Built-In Checks

## Rationale

The validation package currently makes a clear distinction between reusable rule identity and one-off ad-hoc errors: built-in assertions are backed by `ValidationErrorDefinition` instances, while imperative call sites can fall back to `AddError(string, ...)`. This keeps the core API coherent, but it leaves an ergonomics gap for common validator authoring scenarios where a caller wants to keep the built-in assertion semantics and defaults while overriding the human-readable message, code, category, metadata, or target inline.

Adding separate `string message`, `string code`, `ErrorCategory category`, and similar overload combinations to every built-in assertion would recreate the overload explosion that the recent validation plans explicitly avoided. The goal of this plan is therefore to introduce one compact override model that works consistently across the built-in assertion catalog, keeps the default path allocation-light, preserves cached reusable definitions, and still allows terse call sites such as `check.IsNotNullOrWhiteSpace("Comment must be present")`.

## Acceptance Criteria

- [ ] A public value type named `ValidationErrorOverrides` is introduced for inline built-in assertion overrides, and assertion overloads use the parameter name `overrides`.
- [ ] `ValidationErrorOverrides` exposes exactly the properties `Message`, `Code`, `Category`, and `Metadata` so callers can override any subset of the final `Error` details that belongs to the error payload without constructing a custom definition for simple scenarios.
- [ ] The override type supports terse message-only call sites, for example `check.IsNotNullOrWhiteSpace("Comment must be present")`, without requiring named arguments or object construction at the call site.
- [ ] Built-in assertion overloads are added consistently across the current assertion families, using one parameter-ordering strategy that avoids ambiguous calls with existing overloads.
- [ ] The existing overloads without overrides remain available and source-compatible.
- [ ] `shortCircuitOnError` remains a separate method parameter and is not folded into the override type.
- [ ] When only non-message override properties such as `Code` or `Category` are supplied, built-in assertions continue to use the `ValidationErrorDefinition` path so message-template rendering and message caching are preserved.
- [ ] When `Message` is overridden, built-in assertions skip template rendering for that failure and materialize the final error from the supplied message plus the effective built-in defaults and explicit overrides.
- [ ] Built-in assertion overloads that take `ValidationErrorOverrides` do not introduce additional allocations on the success path compared to the existing overloads, aside from the caller-supplied override value itself.
- [ ] The plan does not re-expand the public `Check<T>.AddError(...)` surface that was intentionally reduced in `0032-7`; the new inline override model applies to built-in assertion overloads, not to a new family of general-purpose `AddError(...)` overloads.
- [ ] XML documentation and API guidance are updated with representative examples for message-only overrides, combined message/code/category overrides, and existing calls that continue to work without overrides.
- [ ] Automated tests cover representative overload resolution, default preservation, unchanged existing target resolution behavior, short-circuit behavior, message-caching behavior, and inline code/category/message override scenarios.

## Technical Details

Introduce a new public value type in `Light.PortableResults.Validation`, named `ValidationErrorOverrides`. The parameter name on assertion methods should be `overrides`, matching the intended call-site language. A `readonly record struct` is an appropriate default because it keeps the type lightweight, allows object initializers and `with` expressions, and does not force heap allocation for the normal pass-by-value case.

The type should expose these properties:

- `string? Message`
- `string? Code`
- `ErrorCategory? Category`
- `MetadataObject? Metadata`

Do not include `Target` in this type. Target ownership should remain with `ValidationContext.Check(...)`, scoped contexts such as `ForMember(...)` / `ForIndex(...)`, and explicit low-level `AddError(...)` calls. Inline built-in assertion overrides should stay focused on the payload of the emitted error, not on moving the failure to a different validation path.

`ValidationErrorOverrides` should be implemented as a `readonly record struct`.

Do not include `shortCircuitOnError` in this type. Short-circuiting is control-flow for fluent validation, not part of the resulting error payload, and callers should continue to see it explicitly in the method signature.

To support the terse message-only case, add an implicit conversion from `string` to `ValidationErrorOverrides`. The conversion should populate `Message` and leave the other members unset. The critical ergonomic requirement is that the following remains possible:

```csharp
context.Check(dto.Comment).IsNotNullOrWhiteSpace("Comment must be present");
```

The richer call-site should look like this:

```csharp
context.Check(dto.Comment).IsNotNullOrWhiteSpace(
    new ValidationErrorOverrides
    {
        Message = "Comment must be present",
        Code = "CommentRequired",
        Category = ErrorCategory.UnprocessableContent
    }
);
```

Do not add a new public `Check<T>.AddError(ValidationErrorDefinition, ValidationErrorOverrides, ...)` overload. The previous plan intentionally reduced `Check<T>.AddError(...)` to three public overload families. Re-expanding that surface would cut against the simplification goal. Instead, add a private `AddBuiltInErrorWithOverrides` helper in `Checks.Helpers.cs` next to the existing `AddBuiltInError`:

```csharp
private static Check<T> AddBuiltInErrorWithOverrides<T>(
    Check<T> check,
    ValidationErrorDefinition definition,
    ValidationErrorOverrides overrides,
    bool shortCircuitOnError
)
```

The helper should first throw `ArgumentException` when all four members are null — a fully empty `ValidationErrorOverrides` is a programmer error (the caller should use the no-override overload instead). It then applies overrides according to two distinct paths:

1. When `overrides.Message` is not set:
   Use `check.AddError(definition, code: overrides.Code, metadata: overrides.Metadata, category: overrides.Category, respectShortCircuit: false)` followed by `ShortCircuitOnErrorIfRequested`. This preserves message-template rendering and message caching for stable definitions while applying the non-message overrides.

2. When `overrides.Message` is set:
   Skip `definition.ProvideMessage(...)` entirely and use `check.AddError(string, ...)` with the effective details merged from the built-in definition and the override object. The effective values should be:
   `message = overrides.Message`
   `code = overrides.Code ?? definition.Code`
   `metadata = overrides.Metadata ?? definition.Metadata`
   `target = definition.Target`
   `category = overrides.Category ?? definition.Category`

This keeps the built-in rule identity and non-message defaults intact while allowing the human-readable message to be replaced without constructing a custom `TemplateValidationErrorDefinition`. Target resolution continues to follow the current built-in definition semantics, which means the check target remains the default unless the built-in definition itself already carries an explicit target.

Both validations must happen before the `check.IsShortCircuited` guard so that programmer errors are surfaced unconditionally, regardless of runtime validation state:

- When all four members are null (default instance), throw `ArgumentException`. Calling the override overload without any overrides is a programmer error; callers should use the no-override overload instead.
- When `overrides.Message` is a non-null but empty or whitespace-only string, throw `ArgumentException`. This matches the contract of `ValidationErrorMessage` and ensures the new overloads do not silently accept values the existing low-level message path would reject.

### Assertion Overload Shape

Keep all existing assertion overloads unchanged. Add one parallel overload family that introduces `ValidationErrorOverrides overrides`. The placement of `overrides` must be consistent and must not create ambiguous calls with existing signatures.

Use the following ordering rules:

1. For assertions whose current signature only contains the checked value plus `shortCircuitOnError`, place `overrides` before `shortCircuitOnError`.

Representative signatures:

```csharp
IsNotNull(this Check<T> check, ValidationErrorOverrides overrides, bool shortCircuitOnError = true)
IsNull(this Check<T> check, ValidationErrorOverrides overrides, bool shortCircuitOnError = true)
IsNotNullOrWhiteSpace(this Check<string> check, ValidationErrorOverrides overrides, bool shortCircuitOnError = false)
IsEmail(this Check<string> check, ValidationErrorOverrides overrides, bool shortCircuitOnError = false)
IsInEnum<TEnum>(this Check<TEnum> check, ValidationErrorOverrides overrides, bool shortCircuitOnError = false)
IsInEnum<TEnum>(this Check<TEnum?> check, ValidationErrorOverrides overrides, bool shortCircuitOnError = false)
```

2. For assertions with required rule parameters and no additional optional rule parameters, place `overrides` after the required rule parameters and before `shortCircuitOnError`.

Representative signatures:

```csharp
HasMinLength(this Check<string> check, int minLength, ValidationErrorOverrides overrides, bool shortCircuitOnError = false)
IsGreaterThan<T>(this Check<T> check, T comparativeValue, ValidationErrorOverrides overrides, bool shortCircuitOnError = false)
HasCount<T>(this Check<IEnumerable<T>> check, int expectedCount, ValidationErrorOverrides overrides, bool shortCircuitOnError = false)
```

3. For assertions that already have additional non-error parameters beyond the core rule identity, keep the common message-only call terse by placing `overrides` before the optional rule parameters and before `shortCircuitOnError`.

Representative signatures:

```csharp
Matches(this Check<string> check, Regex regex, ValidationErrorOverrides overrides, bool shortCircuitOnError = false)
Matches(
    this Check<string> check,
    string pattern,
    ValidationErrorOverrides overrides,
    RegexOptions options = RegexOptions.None,
    bool shortCircuitOnError = false
)

IsEnumName<TEnum>(
    this Check<string?> check,
    ValidationErrorOverrides overrides,
    bool ignoreCase = false,
    bool shortCircuitOnError = false
)

HasPrecisionAndScale(
    this Check<decimal> check,
    int precision,
    int scale,
    ValidationErrorOverrides overrides,
    bool ignoreTrailingZeros = false,
    bool shortCircuitOnError = false
)
```

This ordering is intentional. It keeps the new common cases concise:

```csharp
check.IsEnumName<OrderStatus>("Status name is invalid");
check.HasPrecisionAndScale(4, 2, "Amount format is invalid");
check.Matches("^[0-9]+$", "Code must contain only digits");
```

At the same time, existing calls such as `check.IsEnumName<OrderStatus>(ignoreCase: true)` and `check.HasPrecisionAndScale(4, 2, ignoreTrailingZeros: true)` remain unambiguous because `ValidationErrorOverrides` must not define implicit conversions from `bool`, `RegexOptions`, or other rule-parameter types.

### Equality and Predicate Special Cases

Assertions with comparer or predicate-specific overloads need explicit parallel overloads rather than trying to share one broad signature.

For equality checks, add:

```csharp
IsEqualTo<T>(this Check<T> check, T comparativeValue, ValidationErrorOverrides overrides, bool shortCircuitOnError = false)
IsEqualTo<T>(this Check<T> check, T comparativeValue, IEqualityComparer<T> equalityComparer, ValidationErrorOverrides overrides, bool shortCircuitOnError = false)
IsNotEqualTo<T>(...)
```

For predicate checks, add overloads that use the built-in predicate definition plus overrides:

```csharp
Must<T>(this Check<T> check, Func<T, bool> predicate, ValidationErrorOverrides overrides, bool shortCircuitOnError = false)
Must<T>(this Check<T> check, Func<ReadOnlyValidationContext, T, bool> predicate, ValidationErrorOverrides overrides, bool shortCircuitOnError = false)
```

Do not add an overload that mixes `ValidationErrorDefinition definition` and `ValidationErrorOverrides overrides` on `Must`. Once a caller has chosen an explicit reusable definition, the existing API surface is already the more precise tool. The override-based overloads are for the built-in predicate failure case.

The `IValidationErrorMessageTemplate`-based `Must` overload (`Must(predicate, IValidationErrorMessageTemplate template, string? code, MetadataObject? metadata, bool shortCircuitOnError)`) is also excluded from the override family. That overload already gives the caller direct control over code and metadata, so an additional override-object variant would not improve clarity.

`Custom(...)` should remain unchanged. It already exposes full imperative control through `ValidationContext.AddError(...)`, so adding override-object overloads there would not improve clarity.

### Families Covered

The new overload family should be added consistently across the existing built-in assertion catalog where a built-in validation definition is used:

- `IsNotNull`, `IsNull`
- all `IsEmpty` / `IsNotEmpty` overloads
- `IsEqualTo`, `IsNotEqualTo`
- `IsGreaterThan`, `IsGreaterThanOrEqualTo`, `IsLessThan`, `IsLessThanOrEqualTo`, `IsIn`, `IsNotIn`, `IsInExclusiveRange`
- `IsNotNullOrWhiteSpace`, `HasMinLength`, `HasMaxLength`, `HasLengthIn`, both `Matches` overloads, `IsEmail`, `ContainsOnlyDigits`, `ContainsOnlyLettersAndDigits`
- all `HasCount`, `HasMinCount`, `HasMaxCount` overloads
- `IsInEnum` (both the non-nullable `Check<TEnum>` and nullable `Check<TEnum?>` overloads), `IsEnumName`
- `HasPrecisionAndScale`
- the built-in-definition `Must` overloads described above

Do not add override-object overloads to APIs whose semantics are not “one built-in rule emits one built-in-definition-backed error on failure”. In particular, this plan should not extend imperative or potentially multi-error APIs such as `Custom(...)`, child-validation and item-validation APIs such as `ValidateChild(...)` / `ValidateItems(...)`, low-level `AddError(...)` APIs, or `Must(...)` overloads where the caller already supplies an explicit `ValidationErrorDefinition`.

### Tests

Automated tests should verify:

- message-only string conversion for representative assertions from several families
- inline `Code` and `Category` overrides without a message override
- message + code + category overrides together
- built-in defaults are preserved when only one property is overridden
- `Metadata` overrides still follow the same precedence rules as `Check<T>.AddError(definition, ...)`
- target resolution remains unchanged and continues to come from the current check scope or the built-in definition itself, not from `ValidationErrorOverrides`
- stable built-in definitions still use message caching when only non-message overrides are present
- overriding `Message` bypasses template rendering and therefore bypasses definition-based message caching for that specific call
- existing overloads continue to resolve correctly for calls that only pass the old optional boolean parameters
- passing `default(ValidationErrorOverrides)` or `new ValidationErrorOverrides()` throws `ArgumentException` regardless of whether the check is short-circuited
- representative special cases such as `Matches(pattern, "message")`, `Matches(pattern, "message", RegexOptions.IgnoreCase)`, `IsEnumName<OrderStatus>("message")`, `HasPrecisionAndScale(4, 2, "message")`, and `IsEqualTo(expected, comparer, "message")`
- an empty or whitespace-only `overrides.Message` value throws `ArgumentException` even when the check is short-circuited

Documentation and XML comments should be updated with guidance that:

- custom reusable rule identity still belongs in `ValidationErrorDefinition`
- `ValidationErrorOverrides` is for lightweight call-site customization of built-in assertions
- `Custom(...)` plus `AddError(string, ...)` remains the imperative path for arbitrary validation logic
