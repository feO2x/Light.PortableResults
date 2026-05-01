# Built-In Validation Error Contracts for OpenAPI

## Rationale

Plan `0040-1-openapi-redesign.md` introduces `IPortableErrorMetadataContractRegistry` in `Light.PortableResults.AspNetCore.OpenApi.ErrorContracts`, which maps error code strings to CLR metadata types so the OpenAPI document transformer can narrow `errors[*].metadata` and `errorDetails[*].metadata` to accurate schemas per code.

The `Light.PortableResults.Validation` package already defines a stable code-plus-metadata taxonomy through its built-in `ValidationErrorDefinition` subclasses (`CountValidationErrorDefinition`, `MinCountValidationErrorDefinition`, `GreaterThanValidationErrorDefinition<T>`, `PatternValidationErrorDefinition`, `EnumNameValidationErrorDefinition<TEnum>`, `PrecisionScaleValidationErrorDefinition`, etc.). The metadata keys are centralized in `ValidationErrorMetadataKeys`. Without this follow-up, every caller who uses built-in validation error definitions has to redeclare contracts the library already owns.

Three aspects of the built-in contracts make a pure CLR-type registration awkward:

1. **Code-level polymorphism.** Built-in comparison and range codes are shared across many `T`s, but the global registry is keyed only by error code. `CreateMetadataValue<T>` in `BuiltInValidationErrorDefinitions.Shared.cs` projects any `T` down to one of `null | boolean | int64 | double | decimal | string` for primitives, so the global code-level contract for a code like `GreaterThan` must document a broad JSON-primitive shape. Endpoint-specific typed helpers introduced by this plan then narrow that broad fallback to the concrete `T` when the application can provide it.
2. **Layering.** `Light.PortableResults.AspNetCore.OpenApi.ErrorContracts` (where `IPortableErrorMetadataContractRegistry` lives, per `0040-1`) does not and should not depend on `Light.PortableResults.Validation`. Conversely, `Light.PortableResults.Validation` is an OpenAPI-agnostic foundation used from messaging, gRPC, and console hosts; it must not take on a `Microsoft.OpenApi` package reference or any direct knowledge of the OpenAPI registry. The built-in contract catalog therefore lives in neither package.
3. **Spec-version dependence.** The polymorphic primitive schema (`null | string | number | integer | boolean`) cannot be authored once for every OpenAPI version. OpenAPI 3.0 has no `null` type and instead expresses nullability via `nullable: true`; OpenAPI 3.1+ uses `{ type: "null" }`. The transformer already branches on `OpenApiSpecVersion` for `const` vs `enum` narrowing and must do the same here.

This plan widens the registry to also accept pre-authored `OpenApiSchema` values produced by a per-code factory, introduces a new bridge package `Light.PortableResults.Validation.OpenApi` that owns the catalog and the opt-in extension, and exposes the built-in error codes as compile-time constants on the validation package itself so callers get IntelliSense and refactor safety even when the OpenAPI package is not in scope.

## Acceptance Criteria

- [x] `PortableErrorMetadataContract` is introduced as a public abstract base class with a library-owned closed set of sealed subclasses in `Light.PortableResults.AspNetCore.OpenApi.ErrorContracts` (alongside `IPortableErrorMetadataContractRegistry`), representing a discriminated union of (a) a CLR `Type` (to be run through the ASP.NET Core schema generator), (b) a per-code `Func<OpenApiSpecVersion, OpenApiSchema>` factory, or (c) the absence of metadata. The base type is `public abstract class PortableErrorMetadataContract` exposing `static FromType(Type metadataType)` and `static FromSchema(Func<OpenApiSpecVersion, OpenApiSchema> schemaFactory)` factory methods and a `static PortableErrorMetadataContract NoMetadata { get; }` singleton. No public `Kind` enum is exposed; the concrete subclass is the discriminator and the transformer dispatches via pattern matching. Three sealed subclasses `PortableErrorMetadataTypeContract`, `PortableErrorMetadataSchemaContract`, and `PortableErrorMetadataNoMetadataContract` carry the respective payloads; the type and factory subclasses expose their payloads as public read-only properties. The factory shape (rather than a static `OpenApiSchema` instance) is mandatory because `OpenApiSchema` is a mutable POCO; storing a single instance in a static catalog leaks mutations across consumer hosts. The factory accepts the resolved `OpenApiSpecVersion` so spec-version-dependent shapes can author themselves correctly; callers that do not need the version simply ignore the parameter. OpenAPI document generation runs only during application startup, so neither the factory invocation nor the abstract-class allocation is perf-sensitive.
- [x] `IPortableErrorMetadataContractRegistry.Contracts` is widened from `IReadOnlyDictionary<string, Type>` to `IReadOnlyDictionary<string, PortableErrorMetadataContract>`. The default implementation and its tests are updated accordingly.
- [x] `PortableErrorMetadataContractsBuilder` gains two new overloads: `ForCode(string code, Func<OpenApiSpecVersion, OpenApiSchema> metadataSchemaFactory)` (storing `PortableErrorMetadataContract.FromSchema(...)`) and `ForCode(string code)` (storing `PortableErrorMetadataContract.NoMetadata` for codes that the runtime never decorates with metadata). The existing `ForCode<TMetadata>(string code)` and `ForCode(string code, Type metadataType)` overloads continue to work unchanged and internally store `PortableErrorMetadataContract.FromType(...)`. Re-registering the same code with an equivalent contract is idempotent; registering the same code with a different contract throws a clear duplicate-contract exception instead of using last-writer-wins.
- [x] `PortableResultsOpenApiDocumentTransformer` in `Light.PortableResults.AspNetCore.OpenApi.Generation` is updated to dispatch on the concrete subclass when materializing registry entries: `PortableErrorMetadataTypeContract` entries go through the ASP.NET Core schema generator as before; `PortableErrorMetadataSchemaContract` entries invoke the factory once per generated metadata component (passing the resolved `OpenApiSpecVersion`), install the produced schema, and `$ref` it from the narrowed code schema; `PortableErrorMetadataNoMetadataContract` entries emit the narrowed envelope without a `metadata` reference at all. Concretely, for type and schema contracts the synthesized extension schema continues to be `{ properties: { code: const, metadata: $ref }, required: [code] }`; for no-metadata contracts the extension schema is `{ properties: { code: const }, required: [code] }`, which leaves `metadata` to inherit from the base schema (open object, nullable). This is faithful to the wire — the runtime simply does not write a `metadata` property for these codes — and matches the canonical envelope's nullability. Schema-based contracts therefore only replace the `metadata` reference target, no-metadata contracts remove it entirely, and the narrowed-envelope `allOf [base, extension]` construction in `CreateCodeSpecificSchema` is otherwise unchanged. All three contract kinds share one component-id namespace.
- [x] Schema-based metadata components are stored under the same naming convention used for type-based metadata: `<BaseSchemaId>__<SanitizedCode>__Metadata` (for example `PortableError__Count__Metadata`), produced by the existing `PortableResultsOpenApiSchemaNaming.CreateMetadataSchemaId(...)` helper. There is no flat `<Code>Metadata` namespace; both contract kinds live in the same component-id space so tools that walk `Components.Schemas` see one rule rather than two.
- [x] A new project `Light.PortableResults.Validation.OpenApi` is added to the solution. It targets .NET 10, sets `<IsAotCompatible>true</IsAotCompatible>`, and project-references both `Light.PortableResults.Validation` and `Light.PortableResults.AspNetCore.OpenApi`. `Light.PortableResults.Validation` itself does **not** gain a `Microsoft.OpenApi` reference and remains OpenAPI-agnostic so non-ASP.NET-Core hosts (messaging, gRPC, console) carry no transitive OpenAPI dependency.
- [x] A public static class `BuiltInValidationErrorContracts` is added to `Light.PortableResults.Validation.OpenApi` with the property `public static IReadOnlyDictionary<string, PortableErrorMetadataContract> Contracts { get; }`. The dictionary contains one entry per built-in validation error code that has a stable framework-level shape:
    - Codes that carry metadata (`Count`, `MinCount`, `MaxCount`, `MinLength`, `MaxLength`, `LengthInRange`, `EqualTo`, `NotEqualTo`, `GreaterThan`, `GreaterThanOrEqualTo`, `LessThan`, `LessThanOrEqualTo`, `InRange`, `NotInRange`, `ExclusiveRange`, `Pattern`, `Enum`, `EnumName`, `PrecisionScale`) are stored as `PortableErrorMetadataSchemaContract` instances whose factory returns a fresh `OpenApiSchema` on each invocation, using the exact JSON property names defined in `ValidationErrorMetadataKeys`.
    - Codes that the framework guarantees emit no metadata (`NotNull`, `Null`, `NotEmpty`, `Empty`, `NotNullOrWhiteSpace`, `Email`, `DigitsOnly`, `LettersAndDigitsOnly`) are stored as `PortableErrorMetadataContract.NoMetadata`. These are included so consumers can opt them into endpoints via `WithErrorCodes` without falling back to the inline `WithErrorMetadata` escape hatch with a synthetic empty type.
    - `Predicate` is intentionally excluded because it is the default code emitted by `Must(...)` overloads (`Checks.Predicate.cs`), which routinely accept caller-supplied `ValidationErrorDefinition` instances with bespoke metadata shapes. A globally registered no-metadata contract for `Predicate` would lock the schema for those flows and conflict with what consumers actually want to document.
- [x] Built-in contract schemas that reference a typed value (`comparativeValue`, `lowerBoundary`, `upperBoundary`) declare that property as a `oneOf` over JSON primitives. The exact branches depend on the resolved `OpenApiSpecVersion` of the document, mirroring the existing `const` vs `enum` branch in the transformer:
    - OpenAPI 3.1+: `oneOf: [{ type: string }, { type: number }, { type: integer }, { type: boolean }, { type: "null" }]`.
    - OpenAPI 3.0: `oneOf: [{ type: string }, { type: number }, { type: integer }, { type: boolean }]` plus `nullable: true` on the parent property; the `null` branch is omitted because OpenAPI 3.0 does not support `type: "null"`.
- [x] A public static class `ValidationErrorCodes` is added to `Light.PortableResults.Validation` exposing `public const string` fields for every built-in code (`Count`, `MinCount`, `MaxCount`, `MinLength`, `MaxLength`, `LengthInRange`, `EqualTo`, `NotEqualTo`, `GreaterThan`, `GreaterThanOrEqualTo`, `LessThan`, `LessThanOrEqualTo`, `InRange`, `NotInRange`, `ExclusiveRange`, `Pattern`, `Enum`, `EnumName`, `PrecisionScale`, `NotNull`, `Null`, `NotEmpty`, `Empty`, `NotNullOrWhiteSpace`, `Email`, `DigitsOnly`, `LettersAndDigitsOnly`, `Predicate`). The existing `BuiltInValidationErrorDefinitions.*` constructors are updated to reference these constants instead of string literals. Because the library is pre-stable, this plan also improves the current runtime code strings for developer experience: `LengthIn` becomes `LengthInRange`, `Matches` becomes `Pattern`, `IsInBetween` becomes `InRange`, and `NotInBetween` becomes `NotInRange`. `ValidationErrorCodes` stays in `Light.PortableResults.Validation` (not the new bridge package) because the constants are independently useful in switch arms, message templates, and inline error metadata even when the OpenAPI package is not referenced.
- [x] A public extension method `RegisterBuiltInValidationErrors(this PortableErrorMetadataContractsBuilder builder)` is added in `Light.PortableResults.Validation.OpenApi`. It iterates `BuiltInValidationErrorContracts.Contracts` and registers each entry into the builder by dispatching on the contract subclass: schema entries call the factory overload, no-metadata entries call `ForCode(string)`, and any future type entries would call the existing `ForCode(string, Type)` overload. `Predicate` is intentionally not registered for the reason described above; consumers who want to document a `Predicate` flow either supply their own `ValidationErrorDefinition` with a custom code and register that, or use the inline `WithErrorMetadata` escape hatch on the relevant endpoint.
- [x] Nine generic CLR record types are added to `Light.PortableResults.Validation.OpenApi` to back per-endpoint narrowing of the polymorphic comparison and range codes: `EqualToMetadata<T>(T ComparativeValue)`, `NotEqualToMetadata<T>(T ComparativeValue)`, `GreaterThanMetadata<T>(T ComparativeValue)`, `GreaterThanOrEqualToMetadata<T>(T ComparativeValue)`, `LessThanMetadata<T>(T ComparativeValue)`, `LessThanOrEqualToMetadata<T>(T ComparativeValue)`, `InRangeMetadata<T>(T LowerBoundary, T UpperBoundary)`, `NotInRangeMetadata<T>(T LowerBoundary, T UpperBoundary)`, and `ExclusiveRangeMetadata<T>(T LowerBoundary, T UpperBoundary)`. These are the only built-in codes whose metadata genuinely varies in `T` across call sites (every other built-in code is shape-fixed: lengths/counts → integer, regex → string + integer, enum → string + boolean, precision/scale → integer + integer + boolean). Property names match `ValidationErrorMetadataKeys` exactly so the schema generator's casing convention produces the wire-correct keys (`comparativeValue`, `lowerBoundary`, `upperBoundary`).
- [x] A static class `BuiltInValidationErrorBuilderExtensions` in `Light.PortableResults.Validation.OpenApi` exposes typed extension methods on both `PortableProblemOpenApiBuilder` and `PortableValidationProblemOpenApiBuilder`: `WithEqualToError<T>()`, `WithNotEqualToError<T>()`, `WithGreaterThanError<T>()`, `WithGreaterThanOrEqualToError<T>()`, `WithLessThanError<T>()`, `WithLessThanOrEqualToError<T>()`, `WithInRangeError<T>()`, `WithNotInRangeError<T>()`, and `WithExclusiveRangeError<T>()`. Each helper is a thin wrapper over the existing inline escape hatch — for example `WithInRangeError<T>()` calls `WithErrorMetadata<InRangeMetadata<T>>(ValidationErrorCodes.InRange)` — so the transformer needs no new code path, the endpoint-scoped component id naming (`PortableError__<Op>__<Status>__<ContentType>__InRange`) is reused, and the resulting schema for the typed bound (`integer` for `T = int`, `string` with `format: date-time` for `T = DateTime`, etc.) comes out of the standard ASP.NET Core schema generator. Endpoints that mix global and narrowed contracts (e.g. `WithErrorCodes(ValidationErrorCodes.NotEmpty, ValidationErrorCodes.LengthInRange).WithInRangeError<int>()`) get the global-component reuse for the polymorphism-free codes and the operation-scoped narrowed component for `InRange`, in the same discriminated `anyOf`.
- [x] `Light.PortableResults.Validation.OpenApi.csproj` adds a package reference to `Microsoft.OpenApi` if not already supplied transitively through `Light.PortableResults.AspNetCore.OpenApi`, and a corresponding `<PackageVersion>` entry is added to `Directory.Packages.props` if missing. `Light.PortableResults.Validation.csproj` is unchanged on the dependency front.
- [x] The `NativeAotMovieRating` sample is updated to reference `Light.PortableResults.Validation.OpenApi`, call `.RegisterBuiltInValidationErrors()` inside `ConfigureErrorMetadataContracts`, and opt its endpoints into the relevant built-in codes via `WithErrorCodes(ValidationErrorCodes.Count, ...)`. At least one endpoint demonstrates a narrowed comparison helper (e.g. `.WithInRangeError<int>()` for the rating endpoint that uses `IsInBetween(1, 5)`) so the sample documents the recommended path for site-specific narrowing of polymorphic comparison codes.
- [x] Automated tests cover:
    - The discriminated-union behavior of `PortableErrorMetadataContract` (factory methods, `NoMetadata` singleton, sealed-subclass payloads, pattern-match scenarios, and no public `Kind` enum).
    - The schema output for every metadata-bearing built-in code (round-tripped against the taxonomy in `ValidationErrorMetadataKeys`).
    - Duplicate-registration behavior: equivalent repeated registrations are idempotent, while conflicting registrations for the same raw code throw a clear error and do not silently use last-writer-wins.
    - The validation-code rename: runtime errors emitted by the renamed built-ins use `LengthInRange`, `Pattern`, `InRange`, and `NotInRange`.
    - The narrowed envelope for no-metadata codes (`NotNull`, `Null`, `NotEmpty`, `Empty`, `NotNullOrWhiteSpace`, `Email`, `DigitsOnly`, `LettersAndDigitsOnly`): the synthesized extension schema constrains `code` only and contains no `metadata` reference.
    - The `oneOf`-over-primitives shape for typed-value codes, asserted separately for OpenAPI 3.0 and OpenAPI 3.1+.
    - A full document-validation pass that generates an OpenAPI 3.0 document containing the built-in catalog and validates it against an OpenAPI 3.0 validator, so any spec violation introduced by the catalog (such as a stray `type: "null"` branch leaking into a 3.0 document) is caught at test time rather than by consumers.
    - A round-trip test that registers the same code via `ForCode<TMetadata>(string)` and via `ForCode(string, Func<OpenApiSpecVersion, OpenApiSchema>)` and asserts the two transformers produce structurally equivalent narrowed envelopes (modulo schema source), so future refactors cannot silently diverge the type-based and schema-based code paths.
    - The `RegisterBuiltInValidationErrors` extension registering the expected set of codes (metadata-bearing plus no-metadata codes; `Predicate` excluded).
    - The typed comparison and range helpers: `WithInRangeError<int>()` produces an endpoint-scoped variant whose metadata schema is `{ lowerBoundary: integer, upperBoundary: integer, required: [lowerBoundary, upperBoundary] }`, and `WithInRangeError<DateTime>()` produces `{ type: string, format: date-time }` for both bounds. Equivalent assertions for the equality, greater/less, not-in-range, and exclusive-range helpers.
    - A mixed-contract endpoint scenario that mirrors the validator example in the design discussion (`IsNotEmpty`, `HasLengthIn(10, 1000)`, `IsInBetween(1, 5)`): registering the validator endpoint with `.WithErrorCodes(ValidationErrorCodes.NotEmpty, ValidationErrorCodes.LengthInRange).WithInRangeError<int>()` produces a discriminated `anyOf` whose `NotEmpty` and `LengthInRange` branches reference the global `PortableError__<Code>` components and whose `InRange` branch references the endpoint-scoped narrowed component.
    - An end-to-end scenario where an endpoint opts into a metadata-bearing built-in code and a no-metadata built-in code (e.g. `Count` and `NotNull`) and the generated OpenAPI document contains the expected narrowed schemas in the discriminated `anyOf`.
- [x] `README.md` is updated to describe the new `Light.PortableResults.Validation.OpenApi` package and its opt-in one-liner, the built-in taxonomy surfaced by `ValidationErrorCodes`, the typed comparison/range helpers (`WithInRangeError<T>`, `WithGreaterThanError<T>`, `WithEqualToError<T>`, etc.) for site-specific narrowing of polymorphic codes, and the fact that user-defined codes continue to register through the existing type-based overloads on the OpenAPI package.

## Technical Details

### Contract Widening

`PortableErrorMetadataContract` is an abstract base class with a library-owned closed set of sealed subclasses rather than a struct. The original draft of this plan used a struct for allocation reasons, but OpenAPI document generation runs only during application startup and is not on any hot path; the closed class hierarchy reads cleanly under pattern matching, makes the concrete runtime type the only discriminator, and avoids the boxing pitfalls of an `enum + nullable payload` struct. It intentionally does not expose a `Kind` enum: a public enum would create a second discriminator that can drift from the payload, and it would imply user extensibility that the transformer cannot honor without a behavior-based custom contract API. Future library-owned variants can still be added as new sealed subclasses. The base type, the three sealed subclasses, the builder overloads, and the registry abstractions stay together in `Light.PortableResults.AspNetCore.OpenApi.ErrorContracts`. The default implementation of `IPortableErrorMetadataContractRegistry` stores entries in a `Dictionary<string, PortableErrorMetadataContract>`. Existing call sites that wrote `Type` directly are updated to wrap with `PortableErrorMetadataContract.FromType(...)`. The public `ForCode<TMetadata>(string)` and `ForCode(string, Type)` overloads are unchanged.

The new `ForCode(string code, Func<OpenApiSpecVersion, OpenApiSchema> metadataSchemaFactory)` overload stores the supplied factory directly in a `PortableErrorMetadataSchemaContract`. The factory shape avoids the cloning question entirely: callers (including the built-in catalog) construct a fresh `OpenApiSchema` per invocation, so no two consumer hosts ever share a mutable schema instance and the registry never has to defensively clone. The spec-version parameter is passed through unchanged from the transformer's per-document resolution.

The new `ForCode(string code)` overload stores `PortableErrorMetadataContract.NoMetadata`, a singleton `PortableErrorMetadataNoMetadataContract` instance. This variant exists for codes whose framework-level definitions guarantee no metadata is ever attached at runtime (`NotNull`, `Null`, `NotEmpty`, `Empty`); registering them lets consumers opt those codes into endpoints via `WithErrorCodes` without falling back to an inline escape hatch.

Duplicate registrations are intentionally fail-fast instead of last-writer-wins. A repeated registration is idempotent only when it represents the same contract: the same CLR metadata `Type`, the shared `NoMetadata` singleton, or the same schema factory delegate instance. Any other second registration for the same raw code throws. This keeps option composition predictable: a library can call `RegisterBuiltInValidationErrors()`, an application can add its own codes, and accidental collisions surface at startup instead of silently changing the generated OpenAPI contract based on registration order. If a future consumer needs deliberate global replacement, add an explicit API such as `ReplaceCode(...)` rather than making all `ForCode(...)` calls overwrite by default.

### Transformer Dispatch

When the transformer in `Light.PortableResults.AspNetCore.OpenApi.Generation` synthesizes the canonical `PortableError__<SanitizedCode>` and `PortableValidationErrorDetail__<SanitizedCode>` schemas, it pattern-matches on the contract:

- For `PortableErrorMetadataTypeContract`, runs the CLR type through the ASP.NET Core schema generator exposed by `OpenApiDocumentTransformerContext` (unchanged behavior). Synthesized extension schema: `{ properties: { code: const, metadata: $ref }, required: [code] }`.
- For `PortableErrorMetadataSchemaContract`, invokes the factory once per generated metadata component (passing the resolved `OpenApiSpecVersion`), installs the produced schema under the existing metadata-component naming convention (`PortableResultsOpenApiSchemaNaming.CreateMetadataSchemaId(...)`, yielding ids like `PortableError__Count__Metadata` and `PortableValidationErrorDetail__Count__Metadata`), and `$ref`s it from the narrowed code schema. Synthesized extension schema: identical to the type-contract case.
- For `PortableErrorMetadataNoMetadataContract`, emits the narrowed envelope without a `metadata` property at all. Synthesized extension schema: `{ properties: { code: const }, required: [code] }`. The `metadata` slot inherits from the base schema (open object, nullable), which is faithful to the wire — the runtime simply does not write a `metadata` property for these codes.

The `allOf [base, extension]` envelope construction in `CreateCodeSpecificSchema` is otherwise unchanged across all three contract kinds. They share one component-id namespace, and tools that walk `Components.Schemas` see one rule rather than three.

### Project Structure

The follow-up should respect the current OpenAPI project slices and adds one new project:

- `Light.PortableResults.AspNetCore.OpenApi.ErrorContracts` contains the contract-registration model (`PortableErrorMetadataContract`, its sealed subclasses, builder overloads, options, and registry implementation).
- `Light.PortableResults.AspNetCore.OpenApi.Generation` contains transformer changes and any internal message helpers needed to materialize schema-based contracts into the document.
- `Light.PortableResults.AspNetCore.OpenApi.Schemas` continues to hold general reusable schema catalog and naming helpers only; the built-in validation contract catalog itself is **not** placed here because it depends on validation-package types.
- `Light.PortableResults.Validation` continues to hold validation primitives. This plan adds `ValidationErrorCodes` (compile-time constants only) so the constants are available even when the OpenAPI package is not referenced.
- `Light.PortableResults.Validation.OpenApi` is a new bridge package that depends on both `Light.PortableResults.Validation` and `Light.PortableResults.AspNetCore.OpenApi`. It hosts `BuiltInValidationErrorContracts` and the `RegisterBuiltInValidationErrors(this PortableErrorMetadataContractsBuilder)` extension. This split keeps `Light.PortableResults.Validation` free of any `Microsoft.OpenApi` reference and respects the layering of OpenAPI as a higher-level concern than core validation.

### Built-In Contract Catalog

`BuiltInValidationErrorContracts.Contracts` is a static readonly `IReadOnlyDictionary<string, PortableErrorMetadataContract>` — one entry per built-in code that has a stable framework-level shape. Metadata-bearing codes are stored as `PortableErrorMetadataSchemaContract` instances whose factory authors a fresh `OpenApiSchema` (with `Type = JsonSchemaType.Object`, the exact property keys from `ValidationErrorMetadataKeys`, and `Required` populated to match) on each invocation. No-metadata codes (`NotNull`, `Null`, `NotEmpty`, `Empty`, `NotNullOrWhiteSpace`, `Email`, `DigitsOnly`, `LettersAndDigitsOnly`) are stored as the shared `PortableErrorMetadataContract.NoMetadata` singleton. Examples of the authored shapes for the metadata-bearing codes:

- `Count` → `{ expectedCount: integer }`.
- `MinCount` → `{ minCount: integer }`.
- `MaxCount` → `{ maxCount: integer }`.
- `MinLength` / `MaxLength` → analogous integer properties.
- `LengthInRange` → `{ minLength: integer, maxLength: integer }`.
- `EqualTo` / `NotEqualTo` / `GreaterThan` / `GreaterThanOrEqualTo` / `LessThan` / `LessThanOrEqualTo` → `{ comparativeValue: <primitive oneOf> }`.
- `InRange` / `NotInRange` / `ExclusiveRange` → `{ lowerBoundary: <primitive oneOf>, upperBoundary: <primitive oneOf> }`.
- `Pattern` → `{ pattern: string, regexOptions: integer }`.
- `Enum` → `{ enumType: string }`.
- `EnumName` → `{ enumType: string, ignoreCase: boolean }`.
- `PrecisionScale` → `{ expectedPrecision: integer, expectedScale: integer, ignoreTrailingZeros: boolean }`.

The `<primitive oneOf>` shape is spec-version-dependent and is produced by a small helper inside `BuiltInValidationErrorContracts`:

- OpenAPI 3.1+:
  ```text
  oneOf:
    - { type: string }
    - { type: number }
    - { type: integer }
    - { type: boolean }
    - { type: "null" }
  ```
- OpenAPI 3.0:
  ```text
  oneOf:
    - { type: string }
    - { type: number }
    - { type: integer }
    - { type: boolean }
  ```
  with `nullable: true` on the parent property (`comparativeValue`, `lowerBoundary`, `upperBoundary`). The `null` branch is omitted because OpenAPI 3.0 does not support `type: "null"`.

Although `CreateMetadataValue<T>` distinguishes between `int64`, `double`, and `decimal` at the wire encoding level, OpenAPI collapses the latter two into `number`, so the catalog deliberately does not author a separate `decimal` branch. Tests assert this collapse explicitly so a future contributor does not "fix" it.

### Typed Helpers for Polymorphic Codes

The catalog's polymorphic `oneOf` is the only honest documentation for a code-level contract that is genuinely polymorphic across call sites — but for a given endpoint, the call site usually pins down a concrete `T` (e.g. `IsInBetween(1, 5)` makes both bounds `int`). To let consumers declare that concrete `T` in one line without writing CLR DTO scaffolding by hand, the bridge package ships nine generic record types and a matching set of typed builder extensions.

The records are pre-defined exactly so consumers do not redeclare them per project. The property names match `ValidationErrorMetadataKeys` (`comparativeValue`, `lowerBoundary`, `upperBoundary`) so the schema generator's casing convention emits the wire-correct keys with no further configuration:

```csharp
namespace Light.PortableResults.Validation.OpenApi;

public sealed record GreaterThanMetadata<T>(T ComparativeValue);
public sealed record GreaterThanOrEqualToMetadata<T>(T ComparativeValue);
public sealed record LessThanMetadata<T>(T ComparativeValue);
public sealed record LessThanOrEqualToMetadata<T>(T ComparativeValue);
public sealed record EqualToMetadata<T>(T ComparativeValue);
public sealed record NotEqualToMetadata<T>(T ComparativeValue);
public sealed record InRangeMetadata<T>(T LowerBoundary, T UpperBoundary);
public sealed record NotInRangeMetadata<T>(T LowerBoundary, T UpperBoundary);
public sealed record ExclusiveRangeMetadata<T>(T LowerBoundary, T UpperBoundary);
```

The builder extensions wrap the existing inline `WithErrorMetadata<TMetadata>(string code)` escape hatch so the transformer needs no new code path:

```csharp
public static class BuiltInValidationErrorBuilderExtensions
{
    public static PortableValidationProblemOpenApiBuilder WithInRangeError<T>(
        this PortableValidationProblemOpenApiBuilder builder) =>
        builder.WithErrorMetadata<InRangeMetadata<T>>(ValidationErrorCodes.InRange);

    public static PortableProblemOpenApiBuilder WithInRangeError<T>(
        this PortableProblemOpenApiBuilder builder) =>
        builder.WithErrorMetadata<InRangeMetadata<T>>(ValidationErrorCodes.InRange);

    // ...the equality, greater/less, not-in-range, and exclusive-range variants follow the same shape.
}
```

Each helper is shipped on both the problem builder and the validation-problem builder so the same narrowing works whether the endpoint emits `application/problem+json` as a generic problem or as a validation problem.

The endpoint from the design discussion becomes:

```csharp
app.MapPost("/movies/{id}/ratings", ...)
   .WithName("CreateRating")
   .ProducesPortableValidationProblem(b => b
       .WithErrorCodes(ValidationErrorCodes.NotEmpty, ValidationErrorCodes.LengthInRange)
       .WithInRangeError<int>());
```

Two of the three codes reuse global `PortableError__<Code>` components (no duplication across endpoints) and the genuinely site-specific one becomes an operation-scoped `PortableError__CreateRating__400__application_problem_json__InRange` with `lowerBoundary: integer` and `upperBoundary: integer`. This is the same component-id and discriminator-mapping path the existing inline escape hatch already produces; the typed helpers only save the consumer from defining and naming a CLR DTO.

These nine polymorphic codes are the only ones that get this treatment. Every other built-in code is shape-fixed (lengths/counts → integer, regex → string + integer, enum → string + boolean, precision/scale → integer + integer + boolean), so the global catalog schema is always exact and no narrowing helpers are needed.

### Package Wiring

`Light.PortableResults.Validation.OpenApi` takes on the `Microsoft.OpenApi` package reference (or inherits it transitively from `Light.PortableResults.AspNetCore.OpenApi`) to author `OpenApiSchema` instances. The reference does not flow back into `Light.PortableResults.Validation`, which remains usable from non-ASP.NET-Core hosts. `RegisterBuiltInValidationErrors` is an extension on `PortableErrorMetadataContractsBuilder` (declared in `Light.PortableResults.AspNetCore.OpenApi.ErrorContracts`), so callers who pull in only `Light.PortableResults.Validation` without the bridge package never see the extension in scope and never pay the OpenAPI cost. Callers who want the built-in contracts add a single project/package reference to `Light.PortableResults.Validation.OpenApi` and the extension lights up via a single `using Light.PortableResults.Validation.OpenApi;`.

### Scope Boundaries

- This plan does not auto-register the built-in contracts from `AddPortableResultsForMinimalApis` / `AddPortableResultsForMvc`. The opt-in is explicit so that consumers who do not use the validation package, or who use custom message templates with bespoke codes, are not forced into an extra contract catalog.
- This plan does not unify `IPortableErrorMetadataContractRegistry` with `IValidationErrorDefinitionCache`. They cover different concerns (documentation vs runtime messaging) and any unification is a separate refactor.
- This plan does not make `PortableErrorMetadataContract` user-extensible. The contract hierarchy is intentionally closed and library-owned; callers can register metadata by CLR type, schema factory, or no-metadata marker through the builder APIs. If future scenarios require fully custom contract behavior, that should be designed as a separate behavior-based extension point rather than by allowing arbitrary subclasses.
- This plan does not ship CLR DTO types that mirror the built-in metadata shapes for the global catalog. Per-code `Func<OpenApiSpecVersion, OpenApiSchema>` factories are the canonical representation for the polymorphic global contracts. The nine generic CLR records added in `Light.PortableResults.Validation.OpenApi` (`EqualToMetadata<T>`, `GreaterThanMetadata<T>`, `InRangeMetadata<T>`, etc.) are scoped to the per-endpoint typed-narrowing helpers and are not used by the global catalog.
- This plan does not derive endpoint OpenAPI contracts from validator types directly (e.g. `ProducesPortableValidationProblemFor<TValidator>()`). The current `Validator<T>` model uses an imperative `PerformValidation` body, and there is no statically discoverable manifest of "which codes with which `T`s." Reaching that ergonomy would require either lifting validators to a declarative form or recording calls under a synthetic context at startup; both are larger refactors and out of scope for this plan.
- This plan does not register `Predicate` in the built-in catalog. `Predicate` is the default code emitted by `Must(...)` overloads (`Checks.Predicate.cs`), which routinely accept caller-supplied `ValidationErrorDefinition` instances with bespoke metadata shapes; a globally registered no-metadata contract for `Predicate` would lock the schema for those flows. Consumers who want to document a `Predicate` flow either register their own contract (typically under a custom code attached to a custom `ValidationErrorDefinition`) or use the inline `WithErrorMetadata` escape hatch on the relevant endpoint.
