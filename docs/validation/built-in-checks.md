# Built-in checks

This page lists every assertion that `Light.PortableResults.Validation` provides for `Check<T>`, together with the error code it produces, the metadata keys it attaches, and its default message. Error codes are constants of `ValidationErrorCodes`, metadata keys are constants of `ValidationErrorMetadataKeys`, and every error has the `Validation` category unless you override it.

**Package:** `Light.PortableResults.Validation` (namespaces `Light.PortableResults.Validation` and `Light.PortableResults.Validation.Definitions`)

## Common behavior

- Every assertion has a `shortCircuitOnError` parameter; see [Short-circuiting](getting-started.md#short-circuiting). It defaults to `false`, except for `IsNotNull` and `IsNull`.
- Every assertion has an overload that accepts `ErrorOverrides` before `shortCircuitOnError`; see [Overriding error details](#overriding-error-details).
- Messages start with the [display name](getting-started.md#targets-and-display-names) and format numbers with the configured culture. You can replace every message template; see [Message templates](configuration.md#message-templates-and-localization).
- Assertions on a short-circuited check do nothing.
- Assertions that need a value — ordering comparisons, ranges, string and decimal checks — throw an `InvalidOperationException` when the checked value is `null`. With the default string normalizer, `null` strings become `""` before any assertion runs, so this only happens for strings when you disable normalization. Guard nullable values with `IsNotNull()` first.
- Ordering comparisons and ranges use `Comparer<T>.Default`, and their boundaries must not be `null`. `IsEqualTo` and `IsNotEqualTo` use `EqualityComparer<T>.Default` unless you pass an `IEqualityComparer<T>`, and they accept `null` on both sides.

## Null and empty

| Check | Applies to | Code | Metadata keys | Default message |
| --- | --- | --- | --- | --- |
| `IsNotNull()` | any `T` | `NotNull` | — | `{displayName} must not be null` |
| `IsNull()` | any `T` | `Null` | — | `{displayName} must be null` |
| `IsNotEmpty()` | `string?`, `Guid`, `IEnumerable` collections, `ImmutableArray<T>` | `NotEmpty` | — | `{displayName} must not be empty` |
| `IsEmpty()` | `string?`, `Guid`, `IEnumerable` collections, `ImmutableArray<T>` | `Empty` | — | `{displayName} must be empty` |
| `IsNotNullOrWhiteSpace()` | `string` | `NotNullOrWhiteSpace` | — | `{displayName} must not be empty or whitespace` |

For `Guid`, empty means `Guid.Empty`. The [automatic null checks](advanced.md#automatic-null-checking) of validators produce the same `NotNull` error.

## Strings

| Check | Code | Metadata keys | Default message |
| --- | --- | --- | --- |
| `HasMinLength(minLength)` | `MinLength` | `minLength` | `{displayName} must be at least {minLength} characters long` |
| `HasMaxLength(maxLength)` | `MaxLength` | `maxLength` | `{displayName} must be at most {maxLength} characters long` |
| `HasLengthIn(minLength, maxLength)` | `LengthInRange` | `minLength`, `maxLength` | `{displayName} must be between {minLength} and {maxLength} characters long` |
| `Matches(pattern, options)` / `Matches(regex)` | `Pattern` | `pattern`, `regexOptions` | `{displayName} has an invalid format` |
| `IsEmail()` | `Email` | — | `{displayName} must be an email address` |
| `ContainsOnlyDigits()` | `DigitsOnly` | — | `{displayName} must contain only digits` |
| `ContainsOnlyLettersAndDigits()` | `LettersAndDigitsOnly` | — | `{displayName} must contain only letters and digits` |
| `IsEnumName<TEnum>(ignoreCase)` | `EnumName` | `enumType`, `ignoreCase` | `{displayName} must be a valid enum name` |

All string checks operate on `Check<string>`. Lengths are counts of UTF-16 code units and must not be negative.

- `Matches` writes the pattern and the numeric value of the `RegexOptions` to the metadata. Prefer the `Regex` overload with a cached or source-generated `Regex` on hot paths.
- `IsEmail` uses a lightweight heuristic: the string must contain exactly one `@` sign that is neither the first nor the last character. This is intentionally permissive — it rejects obviously invalid addresses without performing full RFC 5321 validation.
- `ContainsOnlyDigits` tests every character with `char.IsDigit`, `ContainsOnlyLettersAndDigits` with `char.IsLetterOrDigit`. An empty string fails both.
- `IsEnumName<TEnum>` succeeds when the string is the name of a member of `TEnum`. `enumType` is the full name of the enum type.

## Comparisons and ranges

| Check | Code | Metadata keys | Default message |
| --- | --- | --- | --- |
| `IsEqualTo(value)` | `EqualTo` | `comparativeValue` | `{displayName} must be equal to {comparativeValue}` |
| `IsNotEqualTo(value)` | `NotEqualTo` | `comparativeValue` | `{displayName} must not be equal to {comparativeValue}` |
| `IsGreaterThan(value)` | `GreaterThan` | `comparativeValue` | `{displayName} must be greater than {comparativeValue}` |
| `IsGreaterThanOrEqualTo(value)` | `GreaterThanOrEqualTo` | `comparativeValue` | `{displayName} must be greater than or equal to {comparativeValue}` |
| `IsLessThan(value)` | `LessThan` | `comparativeValue` | `{displayName} must be less than {comparativeValue}` |
| `IsLessThanOrEqualTo(value)` | `LessThanOrEqualTo` | `comparativeValue` | `{displayName} must be less than or equal to {comparativeValue}` |
| `IsInRange(lower, upper)` | `InRange` | `lowerBoundary`, `upperBoundary` | `{displayName} must be between {lowerBoundary} and {upperBoundary}` |
| `IsNotInRange(lower, upper)` | `NotInRange` | `lowerBoundary`, `upperBoundary` | `{displayName} must not be between {lowerBoundary} and {upperBoundary}` |
| `IsInExclusiveRange(lower, upper)` | `ExclusiveRange` | `lowerBoundary`, `upperBoundary` | `{displayName} must be between {lowerBoundary} and {upperBoundary} (exclusive)` |

These checks work for every `T`, including numbers, `decimal`, `DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`, `TimeSpan`, `Guid`, enums, and strings. `IsInRange` and `IsNotInRange` include both boundaries. The metadata values keep the type of `T`: integers become `Int64`, `ulong` becomes `UInt64`, `decimal` stays `Decimal`, enum values become their name, and dates, times, GUIDs, and URIs use their [metadata kind](../metadata.md#value-kinds).

## Counts

| Check | Applies to | Code | Metadata keys | Default message |
| --- | --- | --- | --- | --- |
| `HasCount(expectedCount)` | `IEnumerable` collections, `ImmutableArray<T>`, `string` | `Count` | `expectedCount` | `{displayName} must contain exactly {expectedCount} item(s)` |
| `HasMinCount(minCount)` | `IEnumerable` collections, `ImmutableArray<T>`, `string` | `MinCount` | `minCount` | `{displayName} must contain at least {minCount} item(s)` |
| `HasMaxCount(maxCount)` | `IEnumerable` collections, `ImmutableArray<T>`, `string` | `MaxCount` | `maxCount` | `{displayName} must contain at most {maxCount} item(s)` |

Counts must not be negative. For strings, the count is the number of UTF-16 code units; prefer the length checks above for strings, because their messages talk about characters.

## Enums, decimals, GUIDs, and dates

| Check | Applies to | Code | Metadata keys | Default message |
| --- | --- | --- | --- | --- |
| `IsInEnum()` | `TEnum`, `TEnum?` | `Enum` | `enumType` | `{displayName} must be a defined enum value` |
| `HasPrecisionAndScale(precision, scale, ignoreTrailingZeros)` | `decimal`, `decimal?` | `PrecisionScale` | `expectedPrecision`, `expectedScale`, `ignoreTrailingZeros` | `{displayName} must not be more than {precision} digits in total, with allowance for {scale} decimals` |
| `IsUuidV7()` | `Guid` | `UuidV7` | — | `{displayName} must be a version 7 UUID` |
| `IsUtc()` | `DateTime` | `Utc` | — | `{displayName} must be represented in UTC` |
| `IsLocal()` | `DateTime` | `Local` | — | `{displayName} must be a local date and time` |
| `IsUnspecified()` | `DateTime` | `Unspecified` | — | `{displayName} must not specify a time zone` |

`IsInEnum` fails for numeric values that are not defined members of the enum. `HasPrecisionAndScale` appends ` when trailing decimal zeros are ignored` to its message when `ignoreTrailingZeros` is `true`.

### `IsUuidV7`

`IsUuidV7` fails with the `UuidV7` error code unless the GUID's RFC 9562 version field is `7` **and** its variant bits are the RFC variant. `Guid.Empty`, version 4 GUIDs, and values with a non-RFC variant fail. Requiring UUIDv7 is useful when clients mint IDs: the leading timestamp keeps client-generated keys roughly sortable and index-friendly. The same invariant is available standalone as `guid.IsUuidV7()` (`GuidExtensions`) when a repository or message handler needs to guard it outside a check chain.

### `IsUtc`, `IsLocal`, and `IsUnspecified`

`IsUtc`, `IsLocal`, and `IsUnspecified` assert the `DateTime.Kind` of the checked value and fail with the `Utc`, `Local`, and `Unspecified` error codes. They partition `DateTimeKind`: every `DateTime` is accepted by exactly one of them. The contract is only the kind of the value the check sees, independent of its origin — `DateTime.UtcNow`, `DateTime.SpecifyKind`, a custom converter, and non-JSON transports can all produce `Utc` just as a JSON payload with a trailing `Z` does.

That matters for JSON requests, because the default `System.Text.Json` converter maps the ISO 8601 forms to kinds like this:

| Wire value | `DateTime.Kind` | Accepted by |
| --- | --- | --- |
| `2026-08-02T10:00:00Z` | `Utc` | `IsUtc` |
| `2026-08-02T10:00:00+00:00` | `Local` | `IsLocal` |
| `2026-08-02T10:00:00+02:00` | `Local` | `IsLocal` |
| `2026-08-02T10:00:00` | `Unspecified` | `IsUnspecified` |

So `IsUtc` effectively requires a trailing `Z` on the wire: an explicit numeric offset is converted to server-local time and deserializes as `Local`, and that includes `+00:00` even though it denotes the same instant as `Z`. Document the `Z` requirement for your clients — a `+00:00` payload is rejected. Conversely, the two `Local` values above only agree on the instant; their wall-clock value depends on the server's time zone, which is rarely what a portable API wants.

`DateTime` values reach these assertions after the per-check normalizer or `ValidationContextOptions.ValueNormalizer` has run — the default `TrimStringNormalizer` passes non-strings through unchanged. Do not install a normalizer that coerces `Unspecified` to `Utc`: it destroys exactly the signal being validated, and `IsUtc` would then pass for every request. Convert to UTC in your mapping code, after validation. Note also that `default(DateTime)` is `Unspecified`, so `IsUnspecified` accepts a missing value; pair it with a range check when that matters.

## Predicates and custom logic

| Check | Code | Default message |
| --- | --- | --- |
| `Must(predicate)` | `Predicate` | `{displayName} is invalid` |
| `Must((readOnlyContext, value) => ...)` | `Predicate` | `{displayName} is invalid` |
| `Must(predicate, template, code, metadata)` | `code`, or `Predicate` | produced by the `IValidationErrorMessageTemplate` |
| `Must(predicate, definition)` | the definition's code | produced by the `ValidationErrorDefinition` |
| `Custom((context, value) => ...)` | whatever the delegate adds | — |

`Must` adds one error when the predicate returns `false`. The overload with a `ReadOnlyValidationContext` enables cross-field checks, for example by inspecting [context items](advanced.md#sharing-state-between-validators). `Custom` runs imperative logic against a scoped `ValidationContext` and may add zero, one, or many errors with `context.AddError(...)`; it has no `shortCircuitOnError` parameter and is not invoked for short-circuited checks. See [Custom assertions](advanced.md#custom-assertions) for examples and for writing reusable assertions.

## Metadata keys

| `ValidationErrorMetadataKeys` constant | Key | Used by |
| --- | --- | --- |
| `ComparativeValue` | `comparativeValue` | `EqualTo`, `NotEqualTo`, `GreaterThan`, `GreaterThanOrEqualTo`, `LessThan`, `LessThanOrEqualTo` |
| `LowerBoundary` | `lowerBoundary` | `InRange`, `NotInRange`, `ExclusiveRange` |
| `UpperBoundary` | `upperBoundary` | `InRange`, `NotInRange`, `ExclusiveRange` |
| `ExpectedCount` | `expectedCount` | `Count` |
| `MinCount` | `minCount` | `MinCount` |
| `MaxCount` | `maxCount` | `MaxCount` |
| `MinLength` | `minLength` | `MinLength`, `LengthInRange` |
| `MaxLength` | `maxLength` | `MaxLength`, `LengthInRange` |
| `Pattern` | `pattern` | `Pattern` |
| `RegexOptions` | `regexOptions` | `Pattern` |
| `EnumType` | `enumType` | `Enum`, `EnumName` |
| `IgnoreCase` | `ignoreCase` | `EnumName` |
| `ExpectedPrecision` | `expectedPrecision` | `PrecisionScale` |
| `ExpectedScale` | `expectedScale` | `PrecisionScale` |
| `IgnoreTrailingZeros` | `ignoreTrailingZeros` | `PrecisionScale` |

`Light.PortableResults.Validation.OpenApi` documents these codes and metadata shapes in OpenAPI; see [OpenAPI](../openapi.md).

<a id="overriding-error-details"></a>

## Overriding error details

Pass `ErrorOverrides` to replace parts of a built-in error at one call site. A plain string converts implicitly to an override of the message:

```csharp
context.Check(dto.Comment).IsNotNullOrWhiteSpace("Comment must be present");

context.Check(dto.Comment).IsNotNullOrWhiteSpace(
    new ErrorOverrides
    {
        Message = "Comment must be present",
        Code = "CommentRequired",
        Category = ErrorCategory.UnprocessableContent
    }
);
```

`ErrorOverrides` has the optional properties `Message`, `Code`, `Category`, and `Metadata`; unset properties keep their built-in values. At least one property must be set, and a set message must not be empty or whitespace — otherwise an `ArgumentException` is thrown.

Use overrides for one-off changes. When the same rule should have a reusable identity across call sites, write a [`ValidationErrorDefinition`](advanced.md#custom-assertions); when the wording should change for the whole application, replace the [message templates](configuration.md#message-templates-and-localization). Overridden codes are not visible to the OpenAPI source generator, which reports [`LPRSG0006`](../openapi.md#lprsg0006) for such calls.
