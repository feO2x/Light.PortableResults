# Functional Extensions for Light.Results

*Please note: this plan was written before the AGENTS.md file in this folder was updated to its current form. Do not
take this plan as an example for how to write plans.*

## Goals & Constraints

1. **Extract functional methods** from `Result<T>` and `Result` structs into extension methods in a new namespace `Light.Results.FunctionalExtensions`.
2. **Unified interface approach**: Create interfaces that both `Result<T>` and `Result` implement, enabling single generic extension methods constrained by these interfaces (similar to `IHasOptionalMetadata<T>` pattern).
3. **Performance**: Maintain zero-allocation paths where possible; avoid boxing structs.
4. **Remove existing methods**: The library is not yet released, so we will remove the original functional methods from the structs entirely (no deprecation needed).
5. **Async with ValueTask**: All async variants use `ValueTask<T>` to avoid allocations when the delegate completes synchronously. Requires `System.Threading.Tasks.Extensions` NuGet package for .NET Standard 2.0.
6. **ConfigureAwait(false)**: All async methods call `.ConfigureAwait(false)` on awaited tasks.

## Current State Analysis

### Existing Functional Methods on `Result<T>`

| Method | Signature | Purpose |
|--------|-----------|---------|
| `Map` | `Result<TOut> Map<TOut>(Func<T, TOut> map)` | Transform value if valid |
| `Bind` | `Result<TOut> Bind<TOut>(Func<T, Result<TOut>> bind, MetadataMergeStrategy)` | Chain to another Result |
| `Tap` | `Result<T> Tap(Action<T> action)` | Side-effect on success |
| `TapError` | `Result<T> TapError(Action<Errors> action)` | Side-effect on failure |

### Existing Functional Methods on `Result` (non-generic)

| Method | Signature | Purpose |
|--------|-----------|---------|
| `TapError` | `Result TapError(Action<Errors> action)` | Side-effect on failure |

### Reference Pattern: `IHasOptionalMetadata<T>`

```csharp
public interface IHasOptionalMetadata<T>
    where T : struct, IHasOptionalMetadata<T>
{
    MetadataObject? Metadata { get; }
    T ReplaceMetadata(MetadataObject? metadata);
}
```

Extension methods in `Tracing.cs` use this pattern:
```csharp
public static T WithSource<T>(this T result, string source)
    where T : struct, IHasOptionalMetadata<T>
```

## Functional Methods to Implement

Based on analysis of ErrorOr, CSharpFunctionalExtensions, and common Result Pattern implementations:

### Category 1: Transformation (Map family)

| Method | Description | Applies To |
|--------|-------------|------------|
| `Map<TOut>` | Transform value on success | `Result<T>` only (needs value) |
| `MapError` | Transform each error on failure | Both |

> **Note**: `MapError` applies the transformation function to each `Error` in the collection individually, returning a new result with the transformed errors. This is more useful than transforming the entire `Errors` collection.

### Category 2: Chaining (Bind family)

| Method | Description | Applies To |
|--------|-------------|------------|
| `Bind<TOut>` | Chain to another `Result<TOut>` | `Result<T>` only |

> **Note**: We use `Bind` instead of `Then` as it is the canonical functional programming term (monadic bind, also known as `flatMap` in Scala/Kotlin or `>>=` in Haskell). `Then` is a fluent API convention but adds redundancy.

### Category 3: Side Effects (Tap family)

| Method | Description | Applies To |
|--------|-------------|------------|
| `Tap` | Execute action on success value | `Result<T>` only |
| `TapError` | Execute action on errors | Both |

### Category 4: Pattern Matching (Match/Switch family)

| Method | Description | Applies To |
|--------|-------------|------------|
| `Match<TOut>` | Return value from success or error handler | Both |
| `MatchFirst<TOut>` | Like Match but error handler receives first error only | Both |
| `Switch` | Execute action for success or error (void) | Both |
| `SwitchFirst` | Like Switch but error handler receives first error only | Both |

### Category 5: Fallback (Else family)

| Method | Description | Applies To |
|--------|-------------|------------|
| `Else` | Provide fallback value on error | `Result<T>` only |

> **Note**: `Else` only applies to `Result<T>` because it provides a fallback *value*. For `Result` (non-generic), the caller can simply use `Result.Ok()` directly if they want to recover from an error.

### Category 6: Conditional (FailIf/Ensure family)

| Method | Description | Applies To |
|--------|-------------|------------|
| `FailIf` | Convert to failure if predicate is true | Both |
| `Ensure` | Convert to failure if predicate is false | Both |

### Category 7: Async Variants

Async variants use `ValueTask<T>` to avoid allocations when delegates complete synchronously. All async methods include `.ConfigureAwait(false)`.

| Sync Method | Async Variant | Async Signature (simplified) |
|-------------|---------------|------------------------------|
| `Map<TOut>` | `MapAsync<TOut>` | `Func<TValue, ValueTask<TOut>>` |
| `MapError` | `MapErrorAsync` | `Func<Error, ValueTask<Error>>` |
| `Bind<TOut>` | `BindAsync<TOut>` | `Func<TValue, ValueTask<Result<TOut>>>` |
| `Tap` | `TapAsync` | `Func<TValue, ValueTask>` |
| `TapError` | `TapErrorAsync` | `Func<Errors, ValueTask>` |
| `Match<TOut>` | `MatchAsync<TOut>` | `Func<TValue, ValueTask<TOut>>`, `Func<Errors, ValueTask<TOut>>` |
| `MatchFirst<TOut>` | `MatchFirstAsync<TOut>` | `Func<TValue, ValueTask<TOut>>`, `Func<Error, ValueTask<TOut>>` |
| `Switch` | `SwitchAsync` | `Func<TValue, ValueTask>`, `Func<Errors, ValueTask>` |
| `SwitchFirst` | `SwitchFirstAsync` | `Func<TValue, ValueTask>`, `Func<Error, ValueTask>` |
| `Else` | `ElseAsync` | `Func<Errors, ValueTask<TValue>>` (Result<T> only) |
| `FailIf` | `FailIfAsync` | `Func<TValue, ValueTask<bool>>` |
| `Ensure` | `EnsureAsync` | `Func<TValue, ValueTask<bool>>` |

> **Dependency**: Requires `System.Threading.Tasks.Extensions` NuGet package for `ValueTask<T>` support in .NET Standard 2.0.

## Interface Design

### Core Interfaces

> **Design Decision**: We use only two self-referencing generic interfaces. A non-generic `IResult` base was considered but rejected because it would allow accidental boxing of `Result<T>` and `Result` structs (e.g., `IResult r = result;`). The self-referencing constraint ensures these interfaces can only be used as generic constraints, keeping structs on the stack.

#### 1. `IResult<TSelf>` - Self-referencing for fluent returns

```csharp
namespace Light.Results;

/// <summary>
/// Result interface that enables fluent extension methods returning the same type.
/// </summary>
/// <typeparam name="TSelf">The implementing type.</typeparam>
public interface IResult<TSelf>
    where TSelf : struct, IResult<TSelf>
{
    /// <summary>Gets whether this result represents a successful operation.</summary>
    bool IsValid { get; }

    /// <summary>Gets the errors collection (empty on success).</summary>
    Errors Errors { get; }

    /// <summary>Gets the first error (throws if no errors).</summary>
    Error FirstError { get; }

    /// <summary>
    /// Creates a successful result with the specified metadata.
    /// </summary>
    static abstract TSelf Ok(MetadataObject? metadata);

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    static abstract TSelf Fail(Errors errors, MetadataObject? metadata);
}
```

#### 2. `IResultWithValue<TSelf, TValue>` - For results that carry a value

```csharp
namespace Light.Results;

/// <summary>
/// Result interface for types that carry a success value.
/// </summary>
/// <typeparam name="TSelf">The implementing type.</typeparam>
/// <typeparam name="TValue">The type of the success value.</typeparam>
public interface IResultWithValue<TSelf, TValue> : IResult<TSelf>
    where TSelf : struct, IResultWithValue<TSelf, TValue>, IResult<TSelf>
{
    /// <summary>Gets the success value (throws if invalid).</summary>
    TValue Value { get; }

    /// <summary>
    /// Creates a successful result with the specified value.
    /// </summary>
    static abstract TSelf Ok(TValue value, MetadataObject? metadata);
}
```

### Interface Implementation

#### `Result<T>` implements:
- `IResult<Result<T>>`
- `IResultWithValue<Result<T>, T>`
- `IHasOptionalMetadata<Result<T>>` (existing)

#### `Result` implements:
- `IResult<Result>`
- `IHasOptionalMetadata<Result>` (existing)

**Note**: `Result` does NOT implement `IResultWithValue` because `Unit` is hidden from callers.

## Extension Method Organization

### File Structure

All extension methods live in a flat folder structure. Each file contains one method pair (sync + async variant) for smaller, focused files.

```
src/Light.Results/
├── FunctionalExtensions/
│   ├── Map.cs                     # Map, MapAsync
│   ├── MapError.cs                # MapError, MapErrorAsync
│   ├── Bind.cs                    # Bind, BindAsync
│   ├── Tap.cs                     # Tap, TapAsync
│   ├── TapError.cs                # TapError, TapErrorAsync
│   ├── Match.cs                   # Match, MatchAsync
│   ├── MatchFirst.cs              # MatchFirst, MatchFirstAsync
│   ├── Switch.cs                  # Switch, SwitchAsync
│   ├── SwitchFirst.cs             # SwitchFirst, SwitchFirstAsync
│   ├── Else.cs                    # Else, ElseAsync
│   ├── FailIf.cs                  # FailIf, FailIfAsync
│   └── Ensure.cs                  # Ensure, EnsureAsync
```

### Namespace

```csharp
namespace Light.Results.FunctionalExtensions;
```

## Implementation Steps

### Phase 1: Create Interfaces

1. Create `IResult{TSelf}.cs` in `src/Light.Results/`
2. Create `IResultWithValue{TSelf,TValue}.cs` in `src/Light.Results/`

### Phase 2: Implement Interfaces on Structs

1. Update `Result<T>` to implement `IResult<Result<T>>`, `IResultWithValue<Result<T>, T>`
2. Update `Result` to implement `IResult<Result>`
3. Add required static abstract factory methods

### Phase 3: Create Extension Methods (Sync + Async)

Order of implementation (by dependency). Each file contains both sync and async variants.

1. **Tap.cs, TapError.cs** - No dependencies, simple side-effects
2. **Map.cs, MapError.cs** - Requires `IResultWithValue` for Map; `IResult<TSelf>` for MapError
3. **Bind.cs** - Requires `IResultWithValue`, builds on Map concepts
4. **Match.cs, MatchFirst.cs, Switch.cs, SwitchFirst.cs** - Requires `IResult<TSelf>` for error path, `IResultWithValue` for value path
5. **Else.cs** - Requires `IResultWithValue` (only for `Result<T>`)
6. **FailIf.cs, Ensure.cs** - Requires `IResult<TSelf>` for creating failures

### Phase 4: Remove Original Methods

Remove the following methods from the structs (no deprecation, library not yet released):

**From `Result<T>`:**
- `Map<TOut>(Func<T, TOut>)`
- `Bind<TOut>(Func<T, Result<TOut>>, MetadataMergeStrategy)`
- `Tap(Action<T>)`
- `TapError(Action<Errors>)`

**From `Result`:**
- `TapError(Action<Errors>)`

### Phase 5: Update Tests

1. Create unit tests for all new extension methods
2. Update existing tests that used the removed methods to use the new extension methods
3. Add tests for edge cases (null delegates, default structs, etc.)

## Sample Extension Method Implementations

### TapError (works on both Result and Result<T>)

```csharp
namespace Light.Results.FunctionalExtensions;

public static class TapExtensions
{
    /// <summary>
    /// Executes the specified action on the errors if this result is invalid.
    /// </summary>
    public static TResult TapError<TResult>(this TResult result, Action<Errors> action)
        where TResult : struct, IResult<TResult>
    {
        if (!result.IsValid)
        {
            action(result.Errors);
        }
        return result;
    }
}
```

### Tap (only for Result<T>)

```csharp
/// <summary>
/// Executes the specified action on the value if this result is valid.
/// </summary>
public static TResult Tap<TResult, TValue>(this TResult result, Action<TValue> action)
    where TResult : struct, IResultWithValue<TResult, TValue>
{
    if (result.IsValid)
    {
        action(result.Value);
    }
    return result;
}
```

### Match (works on both, but value handler differs)

```csharp
/// <summary>
/// Matches the result to a value using the appropriate handler.
/// </summary>
public static TOut Match<TResult, TValue, TOut>(
    this TResult result,
    Func<TValue, TOut> onSuccess,
    Func<Errors, TOut> onError)
    where TResult : struct, IResultWithValue<TResult, TValue>
{
    return result.IsValid
        ? onSuccess(result.Value)
        : onError(result.Errors);
}

/// <summary>
/// Matches the result to a value using the appropriate handler (for void results).
/// </summary>
public static TOut Match<TResult, TOut>(
    this TResult result,
    Func<TOut> onSuccess,
    Func<Errors, TOut> onError)
    where TResult : struct, IResult<TResult>
{
    return result.IsValid
        ? onSuccess()
        : onError(result.Errors);
}
```

### Ensure

```csharp
/// <summary>
/// Returns a failure if the predicate returns false for the value.
/// </summary>
public static TResult Ensure<TResult, TValue>(
    this TResult result,
    Func<TValue, bool> predicate,
    Error error)
    where TResult : struct, IResultWithValue<TResult, TValue>, IHasOptionalMetadata<TResult>
{
    if (!result.IsValid)
    {
        return result;
    }

    return predicate(result.Value)
        ? result
        : TResult.Fail(new Errors(error), result.Metadata);
}
```

## Considerations

### Static Abstract Members

The library uses C# 14, which fully supports `static abstract` members in interfaces. Even though the library targets .NET Standard 2.0, the C# language version is independent of the target framework. Static abstract members work correctly because:

1. The compiler generates the necessary IL that .NET Standard 2.0 runtimes can execute.
2. The constraint resolution happens at compile time.

We will use `static abstract` factory methods in interfaces as designed.

### Metadata Preservation

All extension methods that create new result instances must preserve metadata from the original result. This is handled by:
- Using `result.Metadata` when creating new instances
- Following the pattern established in `Bind` for metadata merging

### Overload Resolution

When both `Result` and `Result<T>` implement `IResult<TSelf>`, ensure extension methods don't cause ambiguity. The `IResultWithValue` constraint naturally separates value-carrying operations.

## Testing Strategy

1. **Unit tests per extension method** - Test success and failure paths
2. **Chaining tests** - Verify fluent chains work correctly
3. **Metadata preservation tests** - Ensure metadata flows through operations
4. **Edge case tests** - Null delegates, default structs, empty errors
5. **Async tests** - Verify async variants work with `ValueTask`
6. **ConfigureAwait tests** - Ensure no deadlocks in sync-over-async scenarios
