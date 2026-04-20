# Built-In Validation Error Contracts for OpenAPI

## Rationale

Plan `0040-1-openapi-redesign.md` introduces `IPortableErrorMetadataContractRegistry`, which maps error code strings to CLR metadata types so the OpenAPI document transformer can narrow `errors[*].metadata` and `errorDetails[*].metadata` to accurate schemas per code.

The `Light.PortableResults.Validation` package already defines a stable code-plus-metadata taxonomy through its built-in `ValidationErrorDefinition` subclasses (`CountValidationErrorDefinition`, `MinCountValidationErrorDefinition`, `GreaterThanValidationErrorDefinition<T>`, `RegexValidationErrorDefinition`, `EnumNameValidationErrorDefinition<TEnum>`, `PrecisionScaleValidationErrorDefinition`, etc.). The metadata keys are centralized in `ValidationErrorMetadataKeys`. Without this follow-up, every caller who uses built-in validation error definitions has to redeclare contracts the library already owns.

Two aspects of the built-in contracts make a pure CLR-type registration awkward:

1. **Polymorphic primitive values.** `CreateMetadataValue<T>` in `BuiltInValidationErrorDefinitions.Shared.cs` projects any `T` down to one of `null | boolean | int64 | double | decimal | string` for primitives. A code like `GreaterThan` is used for integers, dates, strings, and more \u2014 the metadata schema for `comparativeValue` is honestly a JSON-primitive union, not a single CLR type.
2. **Package boundary.** `Light.PortableResults.AspNetCore.OpenApi` (where `IPortableErrorMetadataContractRegistry` lives, per `0040-1`) does not and should not depend on `Light.PortableResults.Validation`. The built-in contracts must live in the validation package and opt in from there.

This plan widens the registry to also accept pre-authored `OpenApiSchema` values, ships a catalog of canonical schemas for every built-in validation error code that carries metadata, and adds a one-line opt-in extension. It also exposes the built-in codes as compile-time constants so callers get IntelliSense and refactor safety when opting into specific codes.

## Acceptance Criteria

- [ ] `PortableErrorMetadataContract` is introduced as a public readonly struct in `Light.PortableResults.AspNetCore.OpenApi` (alongside `IPortableErrorMetadataContractRegistry`), representing a discriminated union of either a CLR `Type` (to be run through the ASP.NET Core schema generator) or a pre-authored `OpenApiSchema`. It exposes `static FromType(Type metadataType)`, `static FromSchema(OpenApiSchema metadataSchema)`, a `Kind` enum property (`Type` / `Schema`), and `TryGetType(out Type)` / `TryGetSchema(out OpenApiSchema)` accessors.
- [ ] `IPortableErrorMetadataContractRegistry.Contracts` is widened from `IReadOnlyDictionary<string, Type>` to `IReadOnlyDictionary<string, PortableErrorMetadataContract>`. The default implementation and its tests are updated accordingly.
- [ ] `PortableErrorMetadataContractsBuilder` gains a new overload `ForCode(string code, OpenApiSchema metadataSchema)`. The existing `ForCode<TMetadata>(string code)` and `ForCode(string code, Type metadataType)` overloads continue to work unchanged and internally store `PortableErrorMetadataContract.FromType(...)`.
- [ ] `PortableResultsOpenApiDocumentTransformer` is updated to dispatch on `PortableErrorMetadataContract.Kind` when materializing registry entries into `Components.Schemas`: `Type` entries go through the ASP.NET Core schema generator as before, `Schema` entries are installed directly.
- [ ] A public static class `BuiltInValidationErrorContracts` is added to `Light.PortableResults.Validation` with the property `public static IReadOnlyDictionary<string, OpenApiSchema> Contracts { get; }`. The dictionary contains hand-authored canonical schemas for every built-in validation error code that carries metadata (`Count`, `MinCount`, `MaxCount`, `Length`, `MinLength`, `MaxLength`, `LengthInRange`, `GreaterThan`, `GreaterThanOrEqualTo`, `LessThan`, `LessThanOrEqualTo`, `InRange`, `Pattern`, `Enum`, `EnumName`, `PrecisionScale`), using the exact JSON property names defined in `ValidationErrorMetadataKeys`.
- [ ] Built-in contract schemas that reference a typed value (`comparativeValue`, `lowerBoundary`, `upperBoundary`) declare that property as a `oneOf` over JSON primitives — `{ type: string }`, `{ type: number }`, `{ type: integer }`, `{ type: boolean }`, `{ type: "null" }` — matching what `CreateMetadataValue<T>` actually produces on the wire.
- [ ] A public static class `ValidationErrorCodes` is added to `Light.PortableResults.Validation` exposing `public const string` fields for every built-in code (e.g. `Count`, `MinCount`, `GreaterThan`, `Pattern`, `EnumName`, `PrecisionScale`, `NotNull`, `NotEmpty`, `Empty`, `Predicate`, ...). The constant values match the code strings assigned in the built-in definition constructors exactly. The existing `BuiltInValidationErrorDefinitions.*` constructors are updated to reference these constants instead of string literals.
- [ ] A public extension method `RegisterBuiltInValidationErrors(this PortableErrorMetadataContractsBuilder builder)` is added in `Light.PortableResults.Validation`. It iterates `BuiltInValidationErrorContracts.Contracts` and calls the new `ForCode(string, OpenApiSchema)` overload for each entry. Codes without metadata (for example `NotNull`, `NotEmpty`, `Empty`, `Predicate`) are intentionally not registered because there is no metadata shape to narrow.
- [ ] `Light.PortableResults.Validation.csproj` adds a package reference to `Microsoft.OpenApi` (the `Microsoft.OpenApi.Models` types only — no ASP.NET Core dependency; the validation package must remain usable from non-ASP.NET Core hosts). The package targets `netstandard2.0` so this reference must be compatible with that target. A corresponding `<PackageVersion>` entry is added to `Directory.Packages.props`.
- [ ] The `NativeAotMovieRating` sample is updated to call `.RegisterBuiltInValidationErrors()` inside `ConfigureErrorMetadataContracts` and to opt its endpoints into the relevant built-in codes via `WithErrorCodes(ValidationErrorCodes.Count, ...)`.
- [ ] Automated tests cover: the discriminated-union behavior of `PortableErrorMetadataContract`, the schema output for every built-in code (round-tripped against the taxonomy in `ValidationErrorMetadataKeys`), the `oneOf`-over-primitives shape for typed-value codes, the `RegisterBuiltInValidationErrors` extension registering the expected set of codes, and an end-to-end scenario where an endpoint opts into a built-in code and the generated OpenAPI document contains the narrowed schema.
- [ ] `README.md` is updated to describe the opt-in one-liner, the built-in taxonomy surfaced by `ValidationErrorCodes`, and the fact that user-defined codes continue to register through the existing type-based overloads.

## Technical Details

### Contract Widening

`PortableErrorMetadataContract` is a small readonly struct, not an interface, so the hot path stays allocation-free. The default implementation of `IPortableErrorMetadataContractRegistry` stores entries in a `Dictionary<string, PortableErrorMetadataContract>`. Existing call sites that wrote `Type` directly are updated to wrap with `PortableErrorMetadataContract.FromType(...)`. The public `ForCode<TMetadata>(string)` and `ForCode(string, Type)` overloads are unchanged.

The new `ForCode(string code, OpenApiSchema metadataSchema)` overload clones the provided schema defensively (using `OpenApiSchema`'s copy constructor) so later mutations by callers do not leak into the registry.

### Transformer Dispatch

When the transformer synthesizes the canonical `PortableError_<Code>` and `PortableValidationErrorDetail_<Code>` schemas, it reads the contract and:

- For `PortableErrorMetadataContract.Kind == Type`, runs the CLR type through the ASP.NET Core schema generator exposed by `OpenApiDocumentTransformerContext` (unchanged behavior).
- For `PortableErrorMetadataContract.Kind == Schema`, installs the provided schema under the name `<Code>Metadata` (if not already present) and `$ref`s it from the narrowed code schema.

Naming for schema-based contracts uses `<Code>Metadata` (for example `CountMetadata`, `GreaterThanMetadata`) so they are discoverable in generated client code and do not collide with user types.

### Built-In Contract Catalog

`BuiltInValidationErrorContracts.Contracts` is built once (static readonly). Each entry authors an `OpenApiSchema` with `Type = "object"`, the exact property keys from `ValidationErrorMetadataKeys`, and `Required` populated to match. Examples of the authored shapes:

- `Count` \u2192 `{ expectedCount: integer }`.
- `MinCount` \u2192 `{ minCount: integer }`.
- `MaxCount` \u2192 `{ maxCount: integer }`.
- `Length` / `MinLength` / `MaxLength` \u2192 analogous integer properties.
- `LengthInRange` \u2192 `{ minLength: integer, maxLength: integer }`.
- `GreaterThan` / `GreaterThanOrEqualTo` / `LessThan` / `LessThanOrEqualTo` \u2192 `{ comparativeValue: <primitive oneOf> }`.
- `InRange` \u2192 `{ lowerBoundary: <primitive oneOf>, upperBoundary: <primitive oneOf> }`.
- `Pattern` \u2192 `{ pattern: string, regexOptions: integer }`.
- `Enum` \u2192 `{ enumType: string }`.
- `EnumName` \u2192 `{ enumType: string, ignoreCase: boolean }`.
- `PrecisionScale` \u2192 `{ expectedPrecision: integer, expectedScale: integer, ignoreTrailingZeros: boolean }`.

The `<primitive oneOf>` shape is a shared helper schema referenced by `$ref` to avoid duplication:

```text
MetadataPrimitiveValue:
  oneOf:
    - { type: string }
    - { type: number }
    - { type: integer }
    - { type: boolean }
    - { type: "null" }
```

`MetadataPrimitiveValue` is registered alongside the per-code schemas and reused wherever a typed primitive metadata value appears.

### Package Wiring

`Light.PortableResults.Validation` takes on a `Microsoft.OpenApi` package reference to author `OpenApiSchema` instances. The reference is compile-time only; the validation package does not reference `Microsoft.AspNetCore.OpenApi` or `Light.PortableResults.AspNetCore.OpenApi` and remains usable from non-ASP.NET Core hosts — `RegisterBuiltInValidationErrors` is an extension on `PortableErrorMetadataContractsBuilder` (declared in `Light.PortableResults.AspNetCore.OpenApi`), so callers who pull in the validation package without the OpenAPI package simply never see the extension in scope. The `RegisterBuiltInValidationErrors` extension method lives in a file under the same namespace as `BuiltInValidationErrorContracts` so a single `using` import exposes the opt-in along with the constants.

### Scope Boundaries

- This plan does not auto-register the built-in contracts from `AddPortableResultsForMinimalApis` / `AddPortableResultsForMvc`. The opt-in is explicit so that consumers who do not use the validation package, or who use custom message templates with bespoke codes, are not forced into an extra contract catalog.
- This plan does not unify `IPortableErrorMetadataContractRegistry` with `IValidationErrorDefinitionCache`. They cover different concerns (documentation vs runtime messaging) and any unification is a separate refactor.
- This plan does not ship CLR DTO types that mirror the built-in metadata shapes. Pre-authored `OpenApiSchema` instances are the canonical representation for these polymorphic contracts.
