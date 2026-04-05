# General Value Normalization

## Rationale

`IStringValueNormalizer` is string-specific, which means callers who need to normalize other value types before validation have no first-class extension point. Replacing it with a generic `IValueNormalizer` interface gives users a single hook to intercept and normalize any `T` value while keeping the built-in implementations purely string-focused. The interface design allows implementations to use `typeof(T) == typeof(string)` so the JIT can eliminate dead branches at specialization time, matching the performance intent of the existing `NormalizeValueIfNecessary` helper.

## Acceptance Criteria

- [ ] `IStringValueNormalizer` is deleted and replaced by `IValueNormalizer` with a single generic method `T Normalize<T>(T value)`.
- [ ] `TrimStringValueNormalizer` is renamed `TrimStringNormalizer` and implements `IValueNormalizer`. The class remains `sealed`. Behavior: when `T` is `string`, trim whitespace and replace `null` with `string.Empty`; when `T` is `object` and the value is a non-null string, trim it; otherwise return the value unchanged. The constructor is public; the static `Instance` property is retained.
- [ ] `NoOpStringValueNormalizer` is renamed `NoOpValueNormalizer` and implements `IValueNormalizer`. The class remains `sealed`. Its `Normalize<T>` unconditionally returns `value`. The constructor is public; the static `Instance` property is retained.
- [ ] `ValidationContextOptions.StringValueNormalizer` is renamed `ValueNormalizer` with type `IValueNormalizer`. Default remains `TrimStringNormalizer.Instance`. The null guard is preserved.
- [ ] Both `Check<T>` overloads on `ValidationContext` replace `IStringValueNormalizer? stringValueNormalizer` with `IValueNormalizer? valueNormalizer`.
- [ ] `ValidationContext.NormalizeStringValue(string?)` is replaced by `NormalizeValue<T>(T value)` which delegates to `Options.ValueNormalizer.Normalize(value)`.
- [ ] The private static `NormalizeValueIfNecessary` helper on `ValidationContext` is deleted. Its call site simplifies to `(valueNormalizer ?? Options.ValueNormalizer).Normalize(value)`.
- [ ] All affected tests are updated to use the new type and member names.
- [ ] Benchmarks are updated to use the new type and member names.

## Technical Details

### New interface

```csharp
public interface IValueNormalizer
{
    T Normalize<T>(T value);
}
```

### `TrimStringNormalizer` branching pattern

Two branches are required to cover both the JIT-specialization case and the boxed-string case:

```csharp
public T Normalize<T>(T value)
{
    // Branch 1: JIT eliminates this for any T != string at specialization time.
    if (typeof(T) == typeof(string))
    {
        var s = Unsafe.As<T, string?>(ref value);
        var result = s?.Trim() ?? string.Empty;
        return Unsafe.As<string, T>(ref result);
    }

    // Branch 2: runtime pattern match — fires when T is object and value is a non-null string.
    // Null objects do not match the pattern and fall through to the return below.
    if (value is string stringValue)
    {
        var trimmed = stringValue.Trim();
        return Unsafe.As<string, T>(ref trimmed);
    }

    return value;
}
```

Note: null → `string.Empty` coercion is intentionally limited to Branch 1 (T is string). When T is object and the value is null, it is not recognizable as a string and passes through unchanged.

### `NoOpValueNormalizer`

```csharp
public T Normalize<T>(T value) => value;
```

No branching required. The singleton pattern (public constructor + static `Instance`) is retained.

### `ValidationContext` changes

Remove the `NormalizeValueIfNecessary` static helper entirely. The call site in the explicit-target `Check<T>` overload becomes:

```csharp
value = (valueNormalizer ?? Options.ValueNormalizer).Normalize(value);
```

Replace the string-specific public helper:

```csharp
// Before
public string? NormalizeStringValue(string? value) => Options.StringValueNormalizer.Normalize(value);

// After
public T NormalizeValue<T>(T value) => Options.ValueNormalizer.Normalize(value);
```
