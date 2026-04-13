# Plan Deviations for 0032 Validation Checks Expansion

This document describes the differences between the original plan (`0032-0-validation-checks.md`) and the actual implementation across all 10 iterations (0032-0 through 0032-9).

## Summary of Iterations

The original plan (0032-0) established the foundation. Nine subsequent iterations refined the implementation:

1. **0032-1**: Message caching for validation error messages
2. **0032-2**: Message cache key optimization (target normalization skipping)
3. **0032-3**: General value normalization (replacing string-specific normalizers)
4. **0032-4**: Validation checkpoints for local error scope tracking
5. **0032-5**: Namespace restructuring with sub-namespaces
6. **0032-6**: Definition and check file restructuring by assertion family
7. **0032-7**: Reduction of `Check<T>.AddError(...)` overloads
8. **0032-8**: Failure overrides (`ErrorOverrides`) for built-in checks
9. **0032-9**: Test suite enhancement and reorganization

## Major Deviations from Original Plan

### 1. Method Renaming: `IsIn`/`IsNotIn` → `IsInBetween`/`IsNotInBetween`

**Original Plan (0032-0)**: The plan specified `IsIn<T>` as an inclusive range assertion and `IsNotIn<T>` as its inverse.

**Actual Implementation**: These methods were renamed to `IsInBetween<T>` and `IsNotInBetween<T>` to make the inclusive-range semantics explicit and avoid confusion with set-based membership checks.

**Commit**: `45a5bd7 refactor: rename IsIn and IsNotIn to IsInBetween and IsNotInBetween for inclusive-range validation`

**Rationale**: The "Between" naming convention is clearer for range comparisons and aligns better with common validation terminology.

### 2. Namespace Dissolution: `Caching` → `Definitions` and `Messaging`

**Original Plan (0032-5)**: The plan originally introduced a `Light.PortableResults.Validation.Caching` sub-namespace containing definition and message cache types.

**Actual Implementation**: The `Caching` namespace was dissolved. Types were redistributed:
- Definition cache types (`IValidationErrorDefinitionCache`, `ValidationErrorDefinitionCache`) moved to `Light.PortableResults.Validation.Definitions`
- Message cache types (`IValidationErrorMessageCache`, `DefaultValidationErrorMessageCache`, `ValidationErrorMessageCacheKey`, `CachedValidationErrorMessage`) moved to `Light.PortableResults.Validation.Messaging`

**Commit**: `62b6d85 refactor: dissolve Caching namespace in Validation project`

**Rationale**: Two cache types (definition and message) with different responsibilities were confusing in a single namespace. Co-locating each cache with its related types improved discoverability.

### 3. Type Renaming: `ValidationErrorOverrides` → `ErrorOverrides`

**Original Plan (0032-8)**: Specified `ValidationErrorOverrides` as the value type name.

**Actual Implementation**: The type was renamed to the simpler `ErrorOverrides` while remaining in the `Light.PortableResults.Validation` namespace.

**Commit**: `5111cc4 chore: rename ValidationErrorOverrides to ErrorOverrides`

**Rationale**: The shorter name is sufficient given the namespace context and avoids redundancy.

### 4. Interface Variance: `IValidationErrorMessageTemplate<TParameter>` Made Contravariant

**Original Plan**: Not specified.

**Actual Implementation**: The interface was made contravariant: `IValidationErrorMessageTemplate<in TParameter>`.

**Commit**: `c183b99 chore: IValidationErrorMessageTemplate<TParameter> is now contravariant`

**Rationale**: This allows greater flexibility in template implementations when dealing with inheritance hierarchies.

### 5. Dependency Injection Integration Added

**Original Plan**: Not specified.

**Actual Implementation**: A `Module.cs` file was added with `AddValidationForPortableResults()` extension method for `IServiceCollection`, integrating with Microsoft.Extensions.DependencyInjection.

**Commit**: `73b277f feat: add Microsoft.Extensions.DependencyInjection integration for Light.PortableResults.Validation`

**Registration Details**:
- `IValidationContextFactory` → `DefaultValidationContextFactory` (singleton)
- `ValidationContextOptions` available both as options pattern and direct singleton

### 6. ValidationErrorMessageCacheKey Restructuring

**Original Plan (0032-2)**: Specified a simplified key structure.

**Actual Implementation**: The cache key was expanded to include `ValidationTarget`, `TargetPrefix`, and `DisplayName` to support target caching alongside messages, eliminating `NormalizeTargetIfNecessary()` calls on cache hits.

**File**: `src/Light.PortableResults.Validation/Messaging/ValidationErrorMessageCacheKey.cs`

**Structure**:
```csharp
public readonly record struct ValidationErrorMessageCacheKey(
    object Provider,
    ValidationTarget Target,
    string TargetPrefix,
    string? DisplayName,
    CultureInfo Culture
)
```

### 7. Messaging Namespace Seemann Nesting

**Original Plan (0032-5)**: Template implementations were to become nested classes within `ValidationErrorTemplates`.

**Actual Implementation**: The nesting pattern was applied as specified:
- `ValidationErrorTemplates.DisplayName`
- `ValidationErrorTemplates.DisplayNameWithComparable`
- `ValidationErrorTemplates.DisplayNameWithRange`
- `ValidationErrorTemplates.DisplayNameWithParameter<TParameter>`
- `ValidationErrorTemplates.DisplayNameWithPrecisionScale`
- `ValidationErrorTemplates.Constant`
- `ValidationErrorTemplates.IgnoreParameter<TParameter>`

This "hide in plain sight" approach reduces IntelliSense clutter while keeping types public and discoverable.

### 8. Check<T>.DisplayName Nullability

**Original Plan (0032-2)**: `DisplayName` should become nullable with `null` meaning "derive from target at message-formatting time".

**Actual Implementation**: Implemented as specified. The property is `string?` and `ValidationContext.Check()` no longer defaults `displayName` to `TargetDescriptor.Input`.

## Minor Deviations

### 1. ComplexDtoValidationBenchmarks Evolution

The original plan did not specify benchmark structure. The implementation evolved through several commits:
- Initial flat DTO benchmarks
- Complex DTO benchmarks with nested validators
- Constructor injection for validators in benchmarks
- Simplified primitive collection validation

### 2. Simplified `IsNotNull` Implementation

**Commit**: `694ee63 chore: simplify IsNotNull check`

The implementation was simplified from the original specification during development.

### 3. Removal of `ValidationEndpointBenchmarks`

**Commit**: `c169677 chore: remove ValidationEndpointBenchmarks`

These benchmarks were removed during the checkpoint implementation phase as they became obsolete.

### 4. `ToErrors()` → `Errors` Property

**Commit**: `a44e63a chore: replace ToErrors() with Errors property in ValidationContext`

A method was replaced with a property for more natural access patterns.

## Plans Implemented As Specified

The following plans were implemented substantially as written:

- **0032-0 (Original)**: Core assertion families (Null, Empty, Equality, Comparable, Strings, Count, Enums, Decimals, Predicate) with minor naming adjustments
- **0032-1 (Message Caching)**: Full message caching infrastructure with `IsMessageStable` and `IValidationErrorMessageCache`
- **0032-3 (General Value Normalization)**: `IValueNormalizer` replacing `IStringValueNormalizer`
- **0032-4 (Validation Checkpoints)**: `ValidationCheckpoint` type with local error scope tracking
- **0032-6 (File Restructuring)**: Assertion-centric file organization for both `Checks` and `BuiltInValidationErrorDefinitions`
- **0032-7 (Overload Reduction)**: Reduced `AddError` surface to three overload families
- **0032-9 (Test Enhancement)**: Sociable unit test pattern adoption with coverage-driven test design

## File Structure Result

The final implementation structure deviates from the original flat layout:

```
src/Light.PortableResults.Validation/
├── Targeting/              # ValidationTarget, IValidationTargetNormalizer
├── Normalization/          # IValueNormalizer, TrimStringNormalizer
├── Messaging/              # Templates, caching, message formatting
├── Definitions/            # ValidationErrorDefinition, BuiltInValidationErrorDefinitions
├── Checks.*.cs             # Assertion families (9 partial files)
├── CheckExtensions.cs      # Child/collection validation
├── ValidationContext.cs    # Main entry point
├── ValidationCheckpoint.cs # Local error scope
└── ErrorOverrides.cs       # Inline override type
```

## Remaining Work from Plan 0032-2

Plan 0032-2 (Message Cache Key Optimization) acceptance criteria were not fully checked off in the plan document, but the implementation shows:
- [x] Cache key restructuring completed
- [x] `DisplayName` nullability implemented
- [x] Target normalization skipping on cache hits implemented
- [ ] Micro benchmark comparing allocation profiles (unclear if completed)

The micro benchmark comparing optimized path vs. previous implementation was specified but may not have been completed.

This is not an issue as we focus on higher-level benchmarks.
