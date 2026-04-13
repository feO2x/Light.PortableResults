# Namespace Restructuring for Light.PortableResults.Validation

> Update: The originally introduced `Light.PortableResults.Validation.Caching` namespace has since been dissolved again. The definition cache types now live in `Light.PortableResults.Validation.Definitions`, and the message cache types now live in `Light.PortableResults.Validation.Messaging`.

## Rationale

The `Light.PortableResults.Validation` namespace currently contains roughly 60 public types in a flat structure. This makes the API surface appear larger than it is in day-to-day use: a consumer writing a typical validator interacts with at most a dozen of those types, but IntelliSense presents all 60 as equals. Types that are primarily implementation details or advanced configuration concerns compete for attention with the core entry points.

The goal is to reorganize the namespace using two complementary techniques: sub-namespaces that group cohesive types as vertical slices, and Mark Seemann's "hide it in plain sight" approach for concrete implementation types that are public by necessity but rarely referenced directly. After the restructuring, the main namespace should contain only the types a developer needs on a typical workday. The other types remain fully public and discoverable, just not in the way.

## Acceptance Criteria

- [x] The main `Light.PortableResults.Validation` namespace is reduced to the following types only: `ValidationContext`, `ReadOnlyValidationContext`, `ValidationContextOptions`, `ValidationContextKey<T>`, `IValidationContextFactory`, `DefaultValidationContextFactory`, `ValidationState`, `ValidationCheckpoint`, `Check<T>`, `Checks`, `CheckExtensions`, `ValidatedValue<T>`, `ValidatedValue`, `BaseValidator<T>`, `Validator<T>`, `Validator<TSource, TValidated>`, `AsyncValidator<T>`, and `AsyncValidator<TSource, TValidated>`.
- [x] All types that are moved to a sub-namespace remain `public`. No type is made `internal` as part of this restructuring.
- [x] A `Light.PortableResults.Validation.Targeting` sub-namespace is created and contains the seven target-related types: `ValidationTarget`, `ValidationTargetSemantics`, `ValidationTargetSemanticsExtensions`, `ValidationTargetCasing`, `ValidationTargets`, `IValidationTargetNormalizer`, and `DefaultValidationTargetNormalizer`.
- [x] A `Light.PortableResults.Validation.Normalization` sub-namespace is created and contains `IValueNormalizer`, `NoOpValueNormalizer`, `TrimStringNormalizer`, `IAutomaticNullErrorProvider`, `DefaultAutomaticNullErrorProvider`, `NoOpAutomaticNullErrorProvider`, and two new static facade classes `ValueNormalizers` and `AutomaticNullErrorProviders`.
- [x] A `Light.PortableResults.Validation.Messaging` sub-namespace is created. The seven concrete template implementation classes (`DisplayNameValidationErrorMessageTemplate`, `DisplayNameWithComparableValidationErrorMessageTemplate`, `DisplayNameWithRangeValidationErrorMessageTemplate`, `DisplayNameWithParameterValidationErrorMessageTemplate<TParameter>`, `DisplayNameWithPrecisionScaleValidationErrorMessageTemplate`, `ConstantValidationErrorMessageTemplate`, and `IgnoreParameterValidationErrorMessageTemplate<TParameter>`) are renamed and become nested public classes inside `ValidationErrorTemplates` (see Technical Details). The remaining messaging types (`IValidationErrorMessageTemplate`, `IValidationErrorMessageTemplate<TParameter>`, `IComparableValidationErrorMessageTemplate`, `IRangeValidationErrorMessageTemplate`, `ValidationErrorMessage`, `ValidationErrorMessageContext<T>`, `ValidationErrorMessageFormatting`, and `ValidationErrorTemplates` itself) move to this sub-namespace as top-level types.
- [x] A `Light.PortableResults.Validation.Definitions` sub-namespace is created and contains: `ValidationErrorDefinition`, `ValidationErrorDefinition<TParameter>`, `TemplateValidationErrorDefinition`, `TemplateValidationErrorDefinition<TParameter>`, `BuiltInValidationErrorDefinitions`, `ValidationErrorMetadataKeys`, `ValidationRange<T>`, and `PrecisionScaleDescriptor`.
- [x] A `Light.PortableResults.Validation.Caching` sub-namespace is created and contains: `IValidationErrorDefinitionCache`, `ValidationErrorDefinitionCache`, `IValidationErrorMessageCache`, `DefaultValidationErrorMessageCache`, `ValidationErrorMessageCacheKey`, and `CachedValidationErrorMessage`.
- [x] The default usage path (`ValidationContextOptions.Default`, `Validator<T>`, `Check<T>`) requires zero additional `using` statements compared to today.
- [x] Source files are reorganized into sub-directories that match the sub-namespaces (e.g., `Targeting/`, `Normalization/`, `Messaging/`, `Definitions/`, `Caching/`).
- [x] Test files that reference only moved types receive mechanical `using` statement additions and compile without further changes. Test files that reference the old concrete template class names by their standalone identifiers (e.g. `new DisplayNameValidationErrorMessageTemplate(...)`) are updated to use the new nested names (e.g. `new ValidationErrorTemplates.DisplayName(...)`); these are non-mechanical renames and must be applied explicitly.

## Technical Details

### File layout

Create one sub-directory per sub-namespace inside `src/Light.PortableResults.Validation/`:

```
src/Light.PortableResults.Validation/
├── Targeting/
│   ├── ValidationTarget.cs
│   ├── ValidationTargetSemantics.cs
│   ├── ValidationTargetSemanticsExtensions.cs
│   ├── ValidationTargetCasing.cs
│   ├── ValidationTargets.cs
│   ├── IValidationTargetNormalizer.cs
│   └── DefaultValidationTargetNormalizer.cs
├── Normalization/
│   ├── IValueNormalizer.cs
│   ├── NoOpValueNormalizer.cs
│   ├── TrimStringNormalizer.cs
│   ├── ValueNormalizers.cs          ← new
│   ├── IAutomaticNullErrorProvider.cs
│   ├── DefaultAutomaticNullErrorProvider.cs
│   ├── NoOpAutomaticNullErrorProvider.cs
│   └── AutomaticNullErrorProviders.cs  ← new
├── Messaging/
│   ├── IValidationErrorMessageTemplate.cs
│   ├── IComparableValidationErrorMessageTemplate.cs
│   ├── IRangeValidationErrorMessageTemplate.cs
│   ├── ValidationErrorMessage.cs
│   ├── ValidationErrorMessageContext.cs
│   ├── ValidationErrorMessageFormatting.cs
│   ├── ValidationErrorTemplates.cs                              ← sealed partial, outer record only
│   ├── ValidationErrorTemplates.DisplayName.cs                 ← nested class
│   ├── ValidationErrorTemplates.DisplayNameWithComparable.cs   ← nested class
│   ├── ValidationErrorTemplates.DisplayNameWithRange.cs        ← nested class
│   ├── ValidationErrorTemplates.DisplayNameWithParameter.cs    ← nested class
│   ├── ValidationErrorTemplates.DisplayNameWithPrecisionScale.cs ← nested class
│   ├── ValidationErrorTemplates.Constant.cs                    ← nested class
│   └── ValidationErrorTemplates.IgnoreParameter.cs             ← nested class
├── Definitions/
│   ├── ValidationErrorDefinition.cs
│   ├── ValidationErrorDefinition.TParameter.cs
│   ├── TemplateValidationErrorDefinition.cs
│   ├── TemplateValidationErrorDefinition.TParameter.cs
│   ├── BuiltInValidationErrorDefinitions.cs
│   ├── ValidationErrorMetadataKeys.cs
│   ├── ValidationRange.cs
│   └── PrecisionScaleDescriptor.cs
└── Caching/
    ├── IValidationErrorDefinitionCache.cs
    ├── ValidationErrorDefinitionCache.cs
    ├── IValidationErrorMessageCache.cs
    ├── DefaultValidationErrorMessageCache.cs
    ├── ValidationErrorMessageCacheKey.cs
    └── CachedValidationErrorMessage.cs
```

### Seemann nesting for message template implementations

The seven concrete template classes move inside `ValidationErrorTemplates` as nested `public sealed class` members. Their names are shortened when nested because the outer type already provides context:

| Old top-level name | New nested name |
|---|---|
| `DisplayNameValidationErrorMessageTemplate` | `ValidationErrorTemplates.DisplayName` |
| `DisplayNameWithComparableValidationErrorMessageTemplate` | `ValidationErrorTemplates.DisplayNameWithComparable` |
| `DisplayNameWithRangeValidationErrorMessageTemplate` | `ValidationErrorTemplates.DisplayNameWithRange` |
| `DisplayNameWithParameterValidationErrorMessageTemplate<TParameter>` | `ValidationErrorTemplates.DisplayNameWithParameter<TParameter>` |
| `DisplayNameWithPrecisionScaleValidationErrorMessageTemplate` | `ValidationErrorTemplates.DisplayNameWithPrecisionScale` |
| `ConstantValidationErrorMessageTemplate` | `ValidationErrorTemplates.Constant` |
| `IgnoreParameterValidationErrorMessageTemplate<TParameter>` | `ValidationErrorTemplates.IgnoreParameter<TParameter>` |

These nested classes remain `public`, so consumers who want to instantiate them by name can do so via `new ValidationErrorTemplates.Constant("...")`. The nested placement hides them from namespace-level IntelliSense while keeping them fully accessible and their relationship to `ValidationErrorTemplates` explicit. The `private static readonly` fields inside `ValidationErrorTemplates` that hold the shared default instances become self-referential: `ValidationErrorTemplates.DisplayName` is instantiated from within `ValidationErrorTemplates` itself.

`ValidationErrorTemplates` is declared `sealed partial record` so each nested class lives in its own file (see the file layout above). The outer record's properties, static default fields, and constructors stay in `ValidationErrorTemplates.cs`; each nested class gets a dedicated `ValidationErrorTemplates.<NestedName>.cs` file that contains only `sealed partial record ValidationErrorTemplates { public sealed class <NestedName> ... }`.

### Static facades for normalization implementations

Because the library targets .NET Standard 2.0, static interface members are not available. Instead, introduce two small static classes that serve as discoverable entry points to the concrete normalizer and provider singletons:

```csharp
// Light.PortableResults.Validation.Normalization
public static class ValueNormalizers
{
    public static IValueNormalizer Trim { get; } = TrimStringNormalizer.Instance;
    public static IValueNormalizer NoOp { get; } = NoOpValueNormalizer.Instance;
}

public static class AutomaticNullErrorProviders
{
    public static IAutomaticNullErrorProvider Default { get; } = DefaultAutomaticNullErrorProvider.Instance;
    public static IAutomaticNullErrorProvider NoOp { get; } = NoOpAutomaticNullErrorProvider.Instance;
}
```

Consumers that need to configure non-default normalization behavior write `ValueNormalizers.NoOp` instead of `NoOpValueNormalizer.Instance`. The concrete singleton types remain public but consumers who only know the interfaces can discover the available options through the facade without memorizing the concrete type names.

### `using` directives required in main-namespace files

Several files that remain in the main namespace reference types that will move to sub-namespaces. These files will need the corresponding `using` directives added. These are internal changes to the library source and do not affect consumers:

- `ValidationContextOptions.cs` — references types from four of the five sub-namespaces: `IValidationTargetNormalizer` and `ValidationTargets` (`Targeting`), `IValueNormalizer`, `IAutomaticNullErrorProvider`, and their concrete defaults (`Normalization`), `ValidationErrorTemplates` (`Messaging`), and `IValidationErrorDefinitionCache` (`Caching`)
- `Check.cs` and `ValidationContext.cs` — reference `ValidationTarget` and related types → add `using Light.PortableResults.Validation.Targeting;`
- `Checks.*.cs` (all partial files) — reference `ValidationRange<T>`, `PrecisionScaleDescriptor`, `BuiltInValidationErrorDefinitions`, and `ValidationErrorMetadataKeys` from `Definitions`; template interfaces from `Messaging`; and cache types from `Caching`
- `BaseValidator.cs`, `Validator.cs`, `AsyncValidator.cs` — reference `IValueNormalizer` and `IAutomaticNullErrorProvider` → add `using Light.PortableResults.Validation.Normalization;`

### Sub-namespace dependency direction

Files in sub-namespaces may freely reference types in the main namespace and in sibling sub-namespaces within the same assembly. There are no circular project references to worry about. Concrete expected cross-namespace `using` needs within the library source:

- `Normalization/` files — `IValueNormalizer.Normalize` takes `ValidationState` (main namespace) → `using Light.PortableResults.Validation;`
- `Messaging/` files — `ValidationErrorMessageContext<T>` and the cache lookup path reference `CachedValidationErrorMessage` and `IValidationErrorMessageCache` (Caching) → `using Light.PortableResults.Validation.Caching;`
- `Definitions/` files — `ValidationErrorDefinition` base class calls into `IValidationErrorDefinitionCache` (Caching) and produces messages via template interfaces (Messaging) → `using` for both `Caching` and `Messaging`

### No changes to `DefaultValidationContextFactory`

`DefaultValidationContextFactory` stays in the main namespace. It is the natural implementation pair of `IValidationContextFactory` and its placement beside the interface keeps the main namespace self-consistent for callers who use DI composition roots.
