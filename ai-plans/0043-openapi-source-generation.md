# Validator-Driven OpenAPI Source Generation

## Rationale

The current OpenAPI implementation requires endpoint authors to repeat error-code information that already exists in their validators. This plan adds a source-generation path that derives that information from the validator and surfaces it as Minimal API endpoint metadata.

The scope is intentionally narrow:

- **Minimal APIs only.** MVC integration is a follow-up.
- **Synchronous validators only.** `Validator<T>` and `Validator<TSource, TValidated>` are supported; `AsyncValidator` and `PerformValidationAsync` are deferred.
- **Top-level statements only.** Within `PerformValidation`, anything nested inside control-flow constructs (`if`, `switch`, `foreach`, etc.) is skipped with a warning diagnostic.
- **Annotated rules only.** Built-in and explicitly annotated check methods are recognized; `Must`, `Predicate`, `Custom`, and `ErrorOverrides`-driven overrides are opaque unless the user supplies explicit hints.

Examples are promoted to a first-class output. The generator inspects each call site, verifies that metadata-bound arguments are compile-time constants, and emits a single validator-level response example on the affected endpoints (`OpenApiResponse.Content[mt].Examples`). Schema-level examples remain reserved for envelope-wide constants like `category: "Validation"`. This materially improves the rendered Scalar and Swagger UI output without any extra annotation effort beyond the rule attributes themselves.

Two pre-1.0 alignment items in `Light.PortableResults.Validation` are folded into this plan: the comparative-range methods rename from `IsInBetween` / `IsNotInBetween` to `IsInRange` / `IsNotInRange` so method names match `ValidationErrorCodes.InRange` / `NotInRange`; and the recognized signatures stay on paired primitive arguments rather than a `Range<T>` struct, because paired arguments resolve cleanly through `SemanticModel.GetConstantValue(...)` while a struct would force builder-chain pattern matching or cross-file symbol following.

## Acceptance Criteria

- [ ] A new source-generator project is added for validation OpenAPI generation. It targets **`netstandard2.0`** because Roslyn analyzers and source generators are loaded into the C# compiler host and must comply with that contract; this is a hard requirement, not a style choice. The project is packaged as a Roslyn analyzer, uses `LangVersion=latest` (with `PolySharp` or hand-written shims for newer language features as needed), and does not add runtime dependencies to applications beyond the existing validation/OpenAPI packages.
- [ ] The generator project does not reference the validation or OpenAPI runtime assemblies as normal assembly references. It resolves public APIs by metadata name and uses linked/shared source only for small constant-only files where sharing is materially safer than duplicating strings. Runtime implementation files are not linked into the generator.
- [ ] The generator is implemented as an `IIncrementalGenerator` and uses `ForAttributeWithMetadataName(...)` to discover marked validators, so unrelated source changes do not invalidate the generation pipeline. Every value flowing through the pipeline implements value-equality (records or `EquatableArray<T>`) so cached steps remain shareable.
- [ ] The analysis pass is structured as a pure function from `(Compilation, INamedTypeSymbol, CancellationToken)` to a plain DTO describing the generated builder calls. The `IIncrementalGenerator` glue is a thin adapter on top, so the analysis is testable directly without driving the full generator harness and reusable for a future MVC analyzer. The analysis honors cancellation throughout syntax and semantic-model work.
- [ ] Public generator-facing attributes are added without violating layering: validation-rule and validation-error-contract attributes live in the validation layer and do not reference `Microsoft.OpenApi`; OpenAPI-specific opt-in attributes live in `Light.PortableResults.Validation.OpenApi`.
- [ ] Built-in validation checks and built-in validation error definitions are annotated so the generator can identify their error code, metadata shape, and method-argument-to-metadata bindings without hard-coded method-name tables.
- [ ] The comparative-range check methods are renamed from `IsInBetween` / `IsNotInBetween` to `IsInRange` / `IsNotInRange` so method names match `ValidationErrorCodes.InRange` / `NotInRange`. All callers in the solution (sample, tests, internal usages) are updated. This is a breaking change accepted pre-1.0.
- [ ] A public static-abstract contract interface is added in `Light.PortableResults.Validation.OpenApi`, allowing generated partial validator classes to expose `ConfigurePortableValidationOpenApi(PortableValidationProblemOpenApiBuilder builder)` without reflection.
- [ ] A Minimal API helper such as `ProducesPortableValidationProblemFor<TValidator>(...)` is added. It calls the generated validator contract and then applies any caller-supplied manual builder configuration.
- [ ] Marking a validator with the OpenAPI generation attribute requires the validator class to be `partial`, non-nested, non-generic, and a direct subclass of `Validator<T>` or `Validator<TSource, TValidated>`. Nested validators, open or closed generic validator types, generic enclosing types, indirect validator inheritance, custom validator base classes, and otherwise unsupported validators produce clear diagnostics. The diagnostic for custom/indirect bases explicitly tells users that v1 requires directly inheriting from `Validator<T>` or `Validator<TSource, TValidated>`. This keeps the first implementation's emitted partial-type shape intentionally simple.
- [ ] The generator supports synchronous `Validator<T>` and `Validator<TSource, TValidated>` by inspecting the `PerformValidation` method body. `AsyncValidator<T>` / `AsyncValidator<TSource, TValidated>` are explicitly out of scope for this plan; marking an async validator with the OpenAPI generation attribute produces a diagnostic that points at the synchronous-validator restriction.
- [ ] The generator recognizes direct `ValidationContext.Check(...)` calls and fluent chains of supported check extension methods. It supports both standalone calls and assignments that consume a `ValidatedValue<T>` returned by a check chain.
- [ ] The generator analyzes only **top-level statements** in the validation method body. Check expressions nested inside `if`, `else`, `switch`, `for`, `foreach`, `while`, `do`, `try`, `using`, lambdas, or local functions are skipped for schema generation and produce a warning diagnostic that suggests lifting the check out of the control-flow construct, adding explicit emitted-error hints, or accepting the gap via `AllowUnknownErrorCodes()`. Local variable declarations and the trailing `checkpoint.ToValidatedValue(...)` are silently allowed.
- [ ] The generator derives built-in no-metadata and fixed-metadata errors as `WithErrorCodes(...)` calls and derives built-in typed comparison/range errors as the existing typed helper calls such as `WithInRangeError<T>()`.
- [ ] The generator supports user-defined validation error definitions and user-defined check extension methods when they are explicitly annotated with generator-facing metadata that identifies the error code and metadata schema.
- [ ] Generator-facing attributes use source-generator-friendly constructor shapes only: primitive values, strings, `Type`, enums, and arrays of those values. Attribute models do not require interpreting arbitrary object graphs or runtime-created values.
- [ ] Constant-valued arguments reach the generated OpenAPI documents as one concrete validator-level response example per generated endpoint response. The generator inspects each metadata-bound argument with `SemanticModel.GetConstantValue(...)`. When a call site's required metadata values resolve to constants, that call site contributes an error entry to the endpoint's example body; when they do not, that call site is omitted from the example and no diagnostic is raised. The schema/code documentation remains complete. The validation OpenAPI bridge supplies envelope-level constants (e.g. `category: "Validation"`) once at registration time.
- [ ] The `Light.PortableResults.Validation.OpenApi` package is extended with the public APIs needed to attach response-level examples: optional example-entry parameters on the existing typed helpers (`WithInRangeError<T>`, `WithGreaterThanError<T>`, etc.), a general-purpose `WithErrorExample(...)` helper for codes without typed shortcuts, and document-transformer support that composes endpoint-declared example entries into one `OpenApiResponse.Content[mediaType].Examples` body as a `JsonNode` tree matching the active `ValidationProblemSerializationFormat`. `RegisterBuiltInValidationErrors()` attaches the envelope-level `category: "Validation"` schema example with an override hook for users who change the default category.
- [ ] `Must(...)`, `Predicate`, and delegate-based validation are treated as opaque unless the failing definition or wrapper method is statically documentable. Unsupported opaque flows produce diagnostics and require an explicit opt-in to unknown errors or explicit emitted-error hints.
- [ ] `Custom(...)` is treated as opaque by default because it can emit zero, one, or many errors. A documented validator that uses `Custom(...)` must either provide explicit emitted-error hints or opt into unknown errors.
- [ ] Generated code is NativeAOT-safe: it does not use reflection, does not instantiate arbitrary validation error definitions for documentation, and does not call ASP.NET Core's runtime schema exporter for generated validation metadata.
- [ ] Generated source uses a controlled, deterministic using block and does not depend on the consumer project's implicit usings, global usings, aliases, or local using directives.
- [ ] Generated source hint names are deterministic and stable across machines and builds, based on the validator's fully qualified metadata name. This keeps IDE behavior and snapshot tests stable.
- [ ] Automatic source-null errors emitted before `PerformValidation` runs are explicitly out of scope. The plan documents that users should add an explicit top-level `IsNotNull()` rule or manual OpenAPI builder configuration when they want source-null errors documented in v1.
- [ ] The `NativeAotMovieRating` sample is updated end to end: `NewMovieRatingValidator` becomes `partial` and is annotated with `[GeneratePortableValidationOpenApi]`; `NewMovieRatingEndpoint` replaces its manual `WithErrorCodes(...)` / `WithInRangeError<int>()` configuration with `ProducesPortableValidationProblemFor<NewMovieRatingValidator>(...)`; an automated test asserts that the resulting OpenAPI document preserves everything the manually configured document had — schemas, error codes, `oneOf` structure, and exhaustiveness flags — after canonical key ordering, while the new document is *additionally* allowed to carry response-level examples that the baseline lacks (the examples are the point of the swap, not a regression); and the rendered Scalar (`/docs`) and Swagger UI (`/swagger/index.html`) outputs both show concrete example values for at least the `InRange` and `LengthInRange` error metadata.
- [ ] Automated tests are added for generator output, diagnostics, Minimal API integration, built-in rule coverage, user-defined rule support, control-flow-skipped checks, constant-vs-non-constant example handling, opaque `Must` / `Custom` handling, and NativeAOT-safe generated code shape. Snapshot tests cover emitted source; document-generation tests cover the resulting OpenAPI document so the generator cannot drift from the transformer contract.
- [ ] Packaging is validated with a local NuGet-package consumer test: `Light.PortableResults.Validation.OpenApi` is packed, consumed from a local feed by a small project without project references to the source tree, and verified to load and run the bundled analyzer/source generator. If this is too slow for the normal unit-test loop, it may live behind a dedicated integration/package test target.
- [ ] Documentation is updated to explain the generator opt-in model, supported validation patterns, the top-level-statements-only analysis boundary, required annotations for custom rules, the constant-argument requirement for examples, limitations around delegates and imperative custom validation, and how to mix generated and manual endpoint OpenAPI configuration.

## Technical Details

### Project Structure

Add a new source-generator project, `Light.PortableResults.Validation.OpenApi.SourceGeneration`, under `src/`. It targets `netstandard2.0` because Roslyn loads analyzer/generator assemblies into the C# compiler host (csc, OmniSharp, the IDE language services) and that host requires `netstandard2.0`. Targeting `net10.0` would either fail to load in many tooling scenarios or trigger `RS1041`. Use `LangVersion=latest` so the generator source itself can use modern C# features; supply `PolySharp` or hand-rolled shims for any BCL types that are not present in `netstandard2.0` (e.g. `IsExternalInit` for records). The csproj sets `<IncludeBuildOutput>false</IncludeBuildOutput>` and references only the Roslyn packages needed for an incremental generator. The generator emits no runtime helpers — generated application code depends only on the runtime packages already used by the application.

**Packaging:** the generator does not ship as its own NuGet. It is bundled into the existing `Light.PortableResults.Validation.OpenApi` package as an analyzer asset: the runtime DLL goes to `lib/`, the generator DLL goes to `analyzers/dotnet/cs/` in the same `.nupkg`. This is the standard pattern for "library + companion generator" (Refit, Mapperly, MediatR.SourceGenerator) and avoids the "I installed the bridge but the generator doesn't run" footgun. The bundling is wired through a `<ProjectReference ... ReferenceOutputAssembly="false" OutputItemType="Analyzer" />` from `Light.PortableResults.Validation.OpenApi.csproj` to the generator project, plus `<None ... PackagePath="analyzers/dotnet/cs">` entries for packing.

The generator project must not take normal assembly references on the validation or validation-OpenAPI runtime assemblies. Analyzer load contexts are intentionally isolated, and depending on runtime DLLs from the analyzer can produce package-only failures even when project-reference builds appear healthy. The generator should resolve public APIs by fully qualified metadata name (`Compilation.GetTypeByMetadataName(...)`) and keep a small internal catalog of known metadata names.

For shared stable strings such as built-in error codes and metadata keys, prefer one of two approaches:

- Link narrow constant-only source files into the generator project when sharing prevents drift and those files have no runtime dependencies.
- Duplicate the strings in a generator-local `KnownValidationNames` / `KnownValidationContracts` style type when linking would pull in implementation concerns.

Do not link runtime implementation-heavy files from `Light.PortableResults.Validation` or `Light.PortableResults.Validation.OpenApi` into the generator. The generator's design-time knowledge should be names and constants, not runtime behavior.

Add a matching test project under `tests/`, for example `Light.PortableResults.Validation.OpenApi.SourceGeneration.Tests`. The tests should compile in-memory source snippets with the generator, assert diagnostics, inspect generated source (snapshot tests using `Verify.SourceGenerators` are appropriate), and run at least a few generated validators through the existing in-memory OpenAPI document test utilities. Tests should also cover the analysis pass directly by calling the pure analysis function with a parsed `Compilation` and asserting the DTO it returns, independent of the generator driver.

Add a package-consumer validation test or script that packs `Light.PortableResults.Validation.OpenApi`, installs it from a local feed into a small temporary consumer project, and builds that project without project references to the source tree. This verifies that the generator is actually included under `analyzers/dotnet/cs/`, that it loads without missing runtime assembly dependencies, and that generated endpoint metadata compiles in the package-consumer scenario.

The solution currently has no Roslyn source-generation infrastructure, so this plan should establish the package layout, test helpers, and central package versions for `Microsoft.CodeAnalysis.CSharp`, `Microsoft.CodeAnalysis.Analyzers`, `Microsoft.CodeAnalysis.CSharp.SourceGenerators.Testing`, and `Verify.SourceGenerators` in a way that can be reused by future generators. Pin a Roslyn version that is widely available across SDKs (avoid pinning to the bleeding edge — generators must run inside the user's chosen SDK).

Analyzer-only package references in the generator project should use `PrivateAssets="all"` and the appropriate `IncludeAssets` metadata so Roslyn/test dependencies do not leak transitively to consumers of `Light.PortableResults.Validation.OpenApi`. The bundled package should expose the runtime OpenAPI bridge normally, but the source generator and its analyzer dependencies remain analyzer implementation details.

### Attribute Model

Keep the attribute model split by concern:

- General validation-rule metadata belongs in `Light.PortableResults.Validation`. These attributes describe validation semantics in OpenAPI-agnostic terms: error code, metadata property names, metadata property value sources, and whether a rule is opaque. They must not reference `Microsoft.OpenApi`.
- OpenAPI source-generation opt-in attributes belong in `Light.PortableResults.Validation.OpenApi`. These attributes mark validators for OpenAPI generation and provide OpenAPI-specific escape hatches such as allowing unknown errors.

The exact names can be refined during implementation, but the shape should support:

```csharp
[GeneratePortableValidationOpenApi]
public sealed partial class NewMovieRatingValidator : Validator<NewMovieRatingDto>
{
    // existing validator implementation
}
```

For validation rules:

```csharp
[ValidationRule(ValidationErrorCodes.InRange)]
[ValidationRuleMetadata(
    ValidationErrorMetadataKeys.LowerBoundary,
    sourceArgument: "lowerBoundary")]
[ValidationRuleMetadata(
    ValidationErrorMetadataKeys.UpperBoundary,
    sourceArgument: "upperBoundary")]
public static Check<T> IsInRange<T>(
    this Check<T> check,
    T lowerBoundary,
    T upperBoundary,
    bool shortCircuitOnError = false)
```

`ValidationRuleMetadataAttribute` supports two forms for resolving a value at generation time: `sourceArgument` for values pulled from a call-site argument by parameter name, and `constantValue` for fixed values that do not depend on the call site. Most built-in rules use `sourceArgument` exclusively; `constantValue` exists for user-defined rules whose definition pins a metadata property to a fixed value. The generator resolves `sourceArgument` values via `SemanticModel.GetConstantValue(...)` on the matching `ArgumentSyntax`; when the value is constant, it flows into the response example.

All generator-facing attributes should stay within compiler-friendly attribute argument types: primitive values, strings, `Type`, enums, and arrays of those values. Do not introduce attribute properties that require the generator to interpret arbitrary objects, runtime-created metadata, delegates, or serialized blobs.

Note that `Error.Category` is a top-level property on the error envelope, not a metadata key. All validation rules produce `ErrorCategory.Validation` by default; users can still override the category at registration or call site. The example value `"Validation"` for the envelope's `category` schema is attached centrally by the validation OpenAPI bridge in `RegisterBuiltInValidationErrors()` and does not appear on per-rule annotations. A user who overrides the category gets that override reflected in the generated example.

For user-defined definitions:

```csharp
[ValidationErrorContract("MovieAlreadyRated")]
[ValidationErrorMetadataProperty("movieId", typeof(Guid))]
public sealed class MovieAlreadyRatedDefinition : ValidationErrorDefinition
{
}
```

For user-defined check methods:

```csharp
[ValidationRule("DivisibleBy", ErrorDefinitionType = typeof(DivisibleByDefinition))]
[ValidationRuleMetadata("divisor", sourceArgument: "divisor")]
public static Check<int> IsDivisibleBy(this Check<int> check, int divisor)
```

The generator should not need to instantiate `ValidationErrorDefinition` types. It should read attributes from symbols and emit OpenAPI builder calls directly. This keeps the source-generation path AOT-compatible and avoids interpreting arbitrary constructor logic.

### Generated Contract Shape

Add a public static-abstract interface in `Light.PortableResults.Validation.OpenApi`, for example:

```csharp
public interface IPortableValidationOpenApiContract
{
    static abstract void ConfigurePortableValidationOpenApi(
        PortableValidationProblemOpenApiBuilder builder);
}
```

For each marked partial validator, the generator emits another partial declaration that implements the interface and configures the builder:

```csharp
public sealed partial class NewMovieRatingValidator : IPortableValidationOpenApiContract
{
    public static void ConfigurePortableValidationOpenApi(
        PortableValidationProblemOpenApiBuilder builder)
    {
        builder.WithErrorCodes(
            ValidationErrorCodes.NotEmpty,
            ValidationErrorCodes.LengthInRange,
            ValidationErrorCodes.NotNullOrWhiteSpace);

        builder.WithInRangeError<int>();
    }
}
```

The first implementation only supports validators that are top-level, non-generic, non-nested, and directly inherit from `Validator<T>` or `Validator<TSource, TValidated>`. This deliberately avoids the complexity of reproducing containing type hierarchies, generic parent declarations, generic constraints, and indirect inheritance chains in generated partial source. Unsupported shapes produce diagnostics and no partial declaration is emitted.

Generated source should use a controlled, deterministic using block for readability. The emitted file must not depend on the consumer project's implicit usings, global usings, aliases, or local using directives; every type used by the generated source must either be covered by the generator's own using block or fully qualified when that is clearer.

Generated source hint names should be stable and deterministic, using the validator's fully qualified metadata name sanitized for file-name safety, for example `NativeAotMovieRating.NewMovieRating.NewMovieRatingValidator.PortableValidationOpenApi.g.cs`.

The generated method should be deterministic, stable across builds, and idempotent with existing builder semantics. It should group simple built-in codes into as few `WithErrorCodes(...)` calls as practical and emit typed helper calls for typed comparison/range rules. For schema configuration, the same error code emitted from multiple call sites in the validator is deduplicated: the example above shows `IsNotEmpty` called twice (on `dto.Id` and `dto.MovieId`) producing a single `NotEmpty` schema entry. Deduplication is by code string, not by call site or target.

Example generation keeps call-site information separate from schema deduplication. A validator-level response example can include both `dto.Id` and `dto.MovieId` as separate error entries even though both use the same `NotEmpty` schema contract.

### Endpoint Integration

Add a Minimal API helper in `Light.PortableResults.Validation.OpenApi`:

```csharp
public static RouteHandlerBuilder ProducesPortableValidationProblemFor<TValidator>(
    this RouteHandlerBuilder builder,
    int statusCode = StatusCodes.Status400BadRequest,
    string contentType = PortableResultsContentTypes.ApplicationProblemJson,
    Action<PortableValidationProblemOpenApiBuilder>? configure = null)
    where TValidator : IPortableValidationOpenApiContract
```

The helper wraps the existing `ProducesPortableValidationProblem(...)` helper. It first calls `TValidator.ConfigurePortableValidationOpenApi(openApiBuilder)` and then invokes the caller-provided `configure` callback so endpoint code can still set the serialization format, top-level metadata, `AllowUnknownErrorCodes()`, or additional manual error metadata.

Example usage:

```csharp
app.MapPut("/api/moviesRatings", AddMovieRating)
   .ProducesPortableValidationProblemFor<NewMovieRatingValidator>(
        configure: builder => builder.UseFormat(ValidationProblemSerializationFormat.Rich));
```

The plan focuses on Minimal APIs first because the current sample and primary OpenAPI workflow use Minimal APIs. MVC support can be added later with either generated static contracts consumed by attributes or a separate MVC-specific plan.

### Validator Analysis Scope

The generator inspects the body of `PerformValidation` declared directly on the marked validator. It does not chase virtual chains: if the marked class inherits a `PerformValidation` from a base validator without overriding it, the generator emits an error diagnostic asking the user to declare (or re-declare) `PerformValidation` on the marked class itself. This keeps the analyzed source local to one file and avoids cross-class symbol traversal that fights Roslyn incrementality. Most validators in practice are sealed and override directly, so this restriction is not expected to bite.

Within the analyzed method, the generator recognizes a deliberately narrow set of patterns. The boundary is **top-level statements only**:

- expression statements of the form `context.Check(...).Assertion1(...).Assertion2(...)...AssertionN(...);`
- assignment statements of the form `target = context.Check(...).Assertion1(...)...AssertionN(...);`, where the right-hand side returns a `Check<T>` or `ValidatedValue<T>`
- local variable declarations whose initializer is a check chain
- the trailing `return checkpoint.ToValidatedValue(...);`
- local variable declarations and trivial expression statements that contain no check chain (these are silently allowed)

Anything nested inside an `if`, `else`, `switch`, `for`, `foreach`, `while`, `do`, `try`, `using`, lambda body, or local function body is **not analyzed for schema generation**. The generator emits a single warning diagnostic per skipped chain that points at the call site and explains the supported workarounds: lift the check out of the control-flow construct so it runs unconditionally, provide explicit emitted-error hints, or accept the missing code on the documented schema by opting into `AllowUnknownErrorCodes()`. This is not "best-effort" control-flow analysis — it is a hard, easily-explained boundary that matches the shape of every validator in the existing sample.

Within an analyzed chain, the generator walks each invocation, looks up `[ValidationRule]` and `[ValidationRuleMetadata]` attributes on the called method, and records:

- the error code emitted by the rule
- each metadata property name, its source (call-site argument or fixed constant), and — when constant — the resolved value
- whether the rule contributes a typed comparison/range error that needs a `WithInRangeError<T>` shape rather than a `WithErrorCodes(...)` entry

The generator infers the example's `target` string for the simple, predictable case: when the chain's leading expression is `context.Check(dto.SomeProperty)` and the argument is a direct member access on the validated source, the target is derived from the accessed property's name. JSON casing follows the configured `JsonNamingPolicy` (camelCase by default in modern ASP.NET Core) and respects `[JsonPropertyName]` if present on the source property. Anything more complex — chained member access (`dto.Sub.Property`), indexer access, method calls, computed expressions — produces a `null` target in the example, which is a documented limitation rather than a diagnostic. This narrow inference is enough to close most of the rendering-quality gap in Scalar and Swagger UI without dragging in real data-flow analysis.

User-defined helper methods are recognized only when annotated themselves with `[ValidationRule(...)]`. The generator does not chase unannotated helpers across files.

Edge case: a bare `context.Check(dto.Property);` call with no subsequent assertion contributes no error codes. The generator silently ignores such calls — they are not malformed, just incomplete validation. No diagnostic is emitted.

Automatic source-null errors emitted by `Validator<T>` / `Validator<TSource, TValidated>` before `PerformValidation` runs are out of scope for this iteration. The generator analyzes the validator body, not the runtime validator pipeline. If users want a source-null error documented in v1, they should add an explicit top-level `IsNotNull()` rule in `PerformValidation` or add the code manually via the endpoint builder.

### Built-In Rule Coverage

Annotate the built-in check extension methods rather than hard-coding method names in the generator. This keeps the generator extensible and gives user-defined check methods the same path as built-ins.

Built-in rule annotations should cover:

- no-metadata codes: `NotNull`, `Null`, `NotEmpty`, `Empty`, `NotNullOrWhiteSpace`, `Email`, `DigitsOnly`, `LettersAndDigitsOnly`
- fixed metadata codes: `Count`, `MinCount`, `MaxCount`, `MinLength`, `MaxLength`, `LengthInRange`, `Pattern`, `Enum`, `EnumName`, `PrecisionScale`
- typed comparison/range codes: `EqualTo`, `NotEqualTo`, `GreaterThan`, `GreaterThanOrEqualTo`, `LessThan`, `LessThanOrEqualTo`, `InRange`, `NotInRange`, `ExclusiveRange`

The recognized signatures keep paired primitive arguments (`IsInRange(min, max)`, `IsGreaterThan(value)`, etc.) so the generator can resolve metadata values via `SemanticModel.GetConstantValue(...)` on each `ArgumentSyntax`. A `Range<T>` struct is intentionally not introduced here: it would force the generator to either pattern-match builder chains or follow stored field symbols across files, both of which fight Roslyn incrementality and complicate constant-value verification. Ergonomic `Range<T>` overloads can be added in the future as separate, generator-unrecognized overloads.

For fixed metadata codes, generated endpoint configuration can use `WithErrorCodes(...)` because global registration via `RegisterBuiltInValidationErrors()` already supplies the metadata schema. For typed comparison/range codes, generated endpoint configuration should use the existing typed helper methods (`WithInRangeError<T>()`, etc.) because the endpoint pins down the concrete `T`.

Renaming `IsInBetween` / `IsNotInBetween` to `IsInRange` / `IsNotInRange` happens in the same change set as adding the rule annotations, so the generator-facing method name, the `[ValidationRule(ValidationErrorCodes.InRange)]` attribute, the error code constant, and the typed helper `WithInRangeError<T>()` all read consistently. Existing tests, the sample, and any documentation in the repo are updated accordingly.

### User-Defined Rules

User-defined rules should follow the same model as built-ins:

1. The error definition declares its stable code and metadata properties through attributes.
2. The check extension method declares which error definition it emits and how method arguments map to metadata properties.
3. The generator reads those attributes and emits either `WithErrorCodes(...)` when a registered global contract is enough, or an inline schema-factory metadata contract when endpoint-specific type narrowing is required.

Generated inline schema factories should be authored with `OpenApiSchema` construction and `PortableOpenApiSchemaTypeMapper`, not with reflection or `JsonSchemaExporter`. If a user-defined metadata property uses an unsupported complex type, the generator falls back to an unconstrained `OpenApiSchema` for that property and emits a warning diagnostic. A future iteration may add an explicit schema-hint attribute for opt-in narrowing of those properties; this plan does not introduce one.

Custom rule annotations need conflict checks:

- Malformed annotations that affect the marked validator are errors, for example metadata bound to a parameter name that does not exist, duplicate metadata keys with incompatible sources, or an error definition contract that omits a required code.
- Two documentable rules used by the same marked validator cannot claim the same error code with incompatible metadata contracts. This produces a diagnostic because the generated endpoint schema would otherwise be ambiguous.
- Compatible duplicate declarations for the same code and metadata shape are allowed and deduplicated at schema-generation time.

### Examples and Constant-Value Verification

Examples are a first-class output of the generator, not an afterthought. The current manually configured documents render correctly but produce thin output in Scalar and Swagger UI: developers see schema shapes without representative values. Because the generator inspects each call site at compile time, it can promote constant arguments into a per-validator **response-level** example — the place OpenAPI 3.x renders concrete bodies most prominently.

Schema-level vs response-level examples:

- Canonical metadata schemas (e.g. the `InRange` metadata schema in the registry) are shared across endpoints. Per-call-site values cannot live on those schemas because they would leak across endpoints. Schema-level `Example` is reserved for envelope-level constants (e.g. `category: "Validation"`) attached centrally at `RegisterBuiltInValidationErrors()` time.
- Per-endpoint concrete values live on `OpenApiResponse.Content[mediaType].Examples`, the OpenAPI 3.x location intended for fully-formed response bodies. Each generated validator/endpoint response produces at most one named `OpenApiExample` entry containing a complete, valid problem-details body with all documentable call-site errors — the same shape the runtime JSON writers produce.

Resolution rules:

- For each metadata property bound by `sourceArgument`, the generator calls `SemanticModel.GetConstantValue(argument)`. When `HasValue` is true, the resolved value is captured and flows into the endpoint's example body. Literal arguments (`1`, `5`, `"^[a-z]+$"`), `const` locals/fields, and constant expressions all flow through naturally.
- For each metadata property bound by `constantValue`, the rule attribute itself supplies the value.
- When a metadata-bearing call site's required arguments do not resolve to constants, that call site is omitted from the response example and no diagnostic is raised. The schema is still complete; only the concrete example entry for that call site is omitted.
- "All metadata or none" applies per call site: if any required metadata value for a call site does not resolve, that call site does not contribute an error entry to the validator-level example. Half-filled bodies confuse Scalar's rendering more than no example entry for that rule.
- No-metadata errors can contribute example entries as long as the generator can build a useful target or intentionally emit `null` for the target. If no call site contributes a useful example entry, no response example is emitted.

Output target: `Microsoft.AspNetCore.OpenApi` 9+ emits OpenAPI 3.1, which prefers the `examples` collection (plural). Generated examples attach through the response-example APIs added in the next subsection. Snapshot tests cover both the JSON document and the rendered Scalar output for the sample to lock the rendering down.

The example feature is intentionally orthogonal to error-code coverage: a validator is still considered fully documented if its codes are all known, even when not all of its arguments resolved to constants.

### OpenAPI Bridge API Extensions for Examples

The existing `Light.PortableResults.Validation.OpenApi` API surface lets callers manipulate metadata schemas but has no way to attach concrete response bodies. Source-generated examples cannot land without extending the bridge first. These extensions are part of this plan, not a separate prerequisite — the generator and the example-emitting API have value only together.

Extensions required:

1. **Builder shortcuts on `PortableValidationProblemOpenApiBuilder` for built-in typed codes.** Existing helpers like `WithInRangeError<T>()` gain optional example-entry parameters: `WithInRangeError<int>(target: "rating", lowerBoundary: 1, upperBoundary: 5)`. When called with values, the helper records both the schema narrowing (current behavior) and an example error entry for the validator-level response example. The same pattern applies to the other typed comparison/range helpers (`WithGreaterThanError<T>`, `WithLessThanOrEqualToError<T>`, etc.).
2. **A general-purpose response-example-entry API** for codes without typed helpers and for user-defined codes: `WithErrorExample(string code, string? target, IReadOnlyDictionary<string, object?>? metadata)`. The metadata dictionary keys match the error's metadata schema; values are constants resolved by the generator (or supplied manually). The bridge composes all endpoint-declared example entries into one complete validator-level error body.
3. **Schema-level envelope examples**, attached centrally by `RegisterBuiltInValidationErrors()`. The validation OpenAPI bridge gains the ability to set `OpenApiSchema.Example` on the canonical envelope's `category` property to `"Validation"`. The registration call accepts an optional override so users who change the default category get a matching example.
4. **Document transformer support.** The transformer reads endpoint metadata for example entries, builds one `OpenApiExample` instance by emitting a `JsonNode` tree shaped like the runtime JSON output (no reflection, no `JsonSerializer` calls — pure node construction), and attaches it to `OpenApiResponse.Content[mediaType].Examples`. Example names are deterministic and response-scoped, for example `"ValidationProblem"` or a stable validator-derived name, so snapshot tests are stable.
5. **Format alignment.** Examples must be valid against the response's selected `ValidationProblemSerializationFormat` (Compact, Rich, AspNetCoreCompatible). The transformer picks the body shape that matches the format the endpoint serves, so what Scalar shows is structurally equivalent to what the endpoint actually returns at runtime.

Example entries are stored as structured endpoint metadata, not as prebuilt response bodies. This matters because `ProducesPortableValidationProblemFor<TValidator>(...)` applies the generated configuration first and the caller's manual configuration afterward. If the caller changes the validation problem format with `UseFormat(...)`, the document transformer composes the final example body after that format is known.

Schema caching remains separate from examples. Equivalent error metadata schemas, such as multiple `InRange<int>` contracts, should reuse the same component-level schema where the existing OpenAPI infrastructure can do so safely. Example bodies are endpoint/validator-level artifacts and preserve the call-site targets and constant values that make the example useful.

These APIs are public and usable without the source generator. Manual callers can supply examples too; the source generator just becomes the most ergonomic path because it derives them from existing validator code.

### `Must`, `Predicate`, `Custom`, and `ErrorOverrides`

Delegate-based and override-based validation is not generally analyzable and must be treated as a boundary:

- `Must(predicate)` with no explicit definition remains opaque because the built-in `Predicate` code intentionally has no stable global metadata contract.
- `Must(predicate, definition)` is documentable only when the supplied definition is statically resolvable and its type has a validation error contract attribute.
- `Custom(...)` is opaque by default because the delegate can add zero, one, or many errors to the context.
- `ErrorOverrides` (any check chain that supplies an `ErrorOverrides` argument to override the default error code or metadata) is **out of scope for this plan**. Arbitrary `MetadataObject` construction is not interpreted at build time, and the override values often come from runtime expressions rather than constants. A future plan may add documentation hints that pair with `ErrorOverrides`, but for now any check using `ErrorOverrides` produces an opaque-flow diagnostic and requires the user to fall back to an explicit emitted-error hint or `AllowUnknownErrorCodes()`.

Provide explicit opt-ins for opaque flows:

- A validator-level or method-level attribute that tells the generator to call `AllowUnknownErrorCodes()`.
- One or more emitted-error hint attributes that explicitly list codes and metadata shapes emitted by custom validation.
- The recommended pattern for reusable predicates is an annotated wrapper check method, e.g. `IsValidSlug(...)`, rather than raw `Must(...)` calls in validators.

Diagnostics should be helpful but not hostile. Opaque flows in a marked validator should produce warnings by default, and generated code should either omit those flows or call `AllowUnknownErrorCodes()` only when the user explicitly opted into that behavior. The generator should not silently weaken an exhaustive schema.

### Diagnostics

The generator emits diagnostics only when a user can act on them. Successful recognition of a rule is silent — the proof is in the generated source, and routine "rule recognized" output would only add noise to IDE error lists.

Severity assignments:

- **Error** — the marked validator cannot be processed at all or the generated endpoint schema would be ambiguous. The generator skips emitting a partial declaration for it when the validator shape itself is unsupported. Triggers: validator class is not `partial`; validator is nested; validator or an enclosing type is generic; validator does not directly inherit from `Validator<T>` or `Validator<TSource, TValidated>`; a custom validator base class sits between the marked validator and `Validator<T>` / `Validator<TSource, TValidated>`; `[GeneratePortableValidationOpenApi]` is applied to an `AsyncValidator<...>`; user-defined check or definition has a malformed attribute combination (e.g. `[ValidationRuleMetadata(sourceArgument: "...")]` referencing a parameter that does not exist); two rules used by the same marked validator claim the same error code with incompatible metadata contracts.
- **Warning** — the validator can be processed, but the generated schema is weaker than the user probably expects. Triggers: opaque `Must` / `Custom` / `ErrorOverrides` flow without an explicit hint; a check chain nested inside control flow was not analyzed; a metadata schema is dropped because a user-defined property uses an unsupported complex type and falls back to an unconstrained schema.
- **Info** — the generator skipped something the user might want to know about, but the skip is design-correct and does not make the schema falsely exhaustive. Triggers: a marked validator yielded zero error codes (likely a misconfiguration but not necessarily wrong).
- **Hidden / no diagnostic** — successful recognition; constant-argument resolution succeeded; no opaque flows present; the validator shape is supported. Silence is the success signal.

Diagnostic ids use the `LPRSG` prefix (Light.PortableResults Source Generator) and live in a single contiguous range (`LPRSG0001`–`LPRSG00xx`). Ids are stable so users can configure them in `.editorconfig`. Every diagnostic carries a help link in the generator's documentation describing the supported workarounds.

### NativeAOT and Performance

Generated code must be startup-only metadata code and must not affect the runtime validation hot path. The generator itself runs at build time; the emitted code runs only when endpoints are mapped and OpenAPI metadata is attached.

NativeAOT requirements:

- no reflection over validators or definitions at runtime
- no generated use of `Type.GetType`, constructor activation, or scanning assemblies
- no schema generation through `JsonSchemaExporter`
- no generated dependency on serializer metadata for generated validation metadata schemas
- generated schema factories create fresh `OpenApiSchema` instances to avoid mutable schema reuse

Source-generator performance is governed by IDE responsiveness, not by build-time wall clock. A source-generator run executes on every keystroke that changes a relevant compilation; the worst regression mode is not slow analysis but **broken caching**, where pipeline values fail value-equality and Roslyn re-runs every step. To stay fast:

- Use `IIncrementalGenerator`. Do not implement the legacy `ISourceGenerator`.
- Discover marked validators via `context.SyntaxProvider.ForAttributeWithMetadataName(...)` so unrelated source changes do not invalidate the pipeline.
- Make every value flowing through the pipeline value-equal: records, sealed classes with overridden `Equals`/`GetHashCode`, or `EquatableArray<T>` (never raw `ImmutableArray<T>`, which uses reference equality).
- **Never let `ISymbol` instances flow past the first pipeline stage.** Symbols hold references to `Compilation`, which is a fresh instance on every keystroke, so any DTO containing an `ISymbol` will compare unequal across runs and silently break the cache. The first node materializes everything it needs (names, metadata key strings, attribute argument values, locations) into a primitives-only DTO; downstream stages never see symbols. This is the single most common source-generator perf bug in the wild.
- Structure the analysis pass as a pure function from `(Compilation, INamedTypeSymbol)` to a plain DTO. The pipeline is then `[validator symbol → primitives DTO] → [emitted source]`, with caching at every stage.
- Avoid touching `Compilation.GetSemanticModel(...)` for unrelated trees. Walk only the marked validator's syntax.

For a typical validator (tens of lines, a flat sequence of check statements), expected per-validator analysis time is single-digit milliseconds against a warm semantic model. With caching working correctly, untouched validators contribute zero work between keystrokes.

A pre-build CLI / MSBuild task is intentionally **not** chosen. CLI tools are appropriate when inputs are external (a YAML, a `.proto`, a remote schema) or when generated output is large and version-pinned. Here the inputs are C# symbols in the same compilation, and the value of the feature is in-IDE feedback: red squigglies on `Must(...)` without a hint, instant compile errors when a generated `WithInRangeError<int>()` call references a renamed helper, diagnostics surfaced in the IDE's error list. A pre-build CLI would forfeit all of that for no real performance benefit.

### Tests

Tests should cover both generator behavior and integrated OpenAPI output:

- generated code for a validator similar to `NewMovieRatingValidator`
- diagnostics for unsupported marked validators: non-partial, nested, generic, indirect inheritance, and async-validator shapes
- diagnostics for unsupported `Must` / `Custom` patterns
- diagnostics for malformed or conflicting custom rule/error-contract annotations
- warning diagnostics for check chains nested inside control flow (`if`, `switch`, `foreach`, etc.) — the chain is skipped, the diagnostic is emitted, and users can resolve it with explicit emitted-error hints or unknown-error opt-in
- built-in typed helper generation for `IsInRange(1, 5)` and similar comparison/range checks
- fixed metadata built-ins such as `HasLengthInRange(10, 1000)`
- user-defined check methods with annotated error definitions and metadata mappings
- explicit unknown-error opt-in producing `AllowUnknownErrorCodes()`
- generated Minimal API endpoint metadata producing the same `oneOf` / metadata schemas as manual builder calls
- a single response-level example appears when at least one call site can contribute a complete example entry; the example body matches the runtime JSON shape for the active `ValidationProblemSerializationFormat`; metadata-bearing call sites with non-constant arguments are omitted from that example while their schemas remain documented
- example `target` strings are derived from `context.Check(dto.Property)` member-access arguments, respect `[JsonPropertyName]` overrides and the configured `JsonNamingPolicy`, and fall back to `null` for complex targets
- envelope-level `category: "Validation"` example appears on the canonical envelope schema and reflects user overrides
- the analysis pass returns equal DTOs across runs for unchanged input, so the incremental pipeline caches correctly (in particular, the DTO contains no `ISymbol` references)
- cancellation is honored by the analysis pass when cancellation is requested during syntax or semantic-model work
- a NativeAOT-oriented generated source assertion that no reflection/schema-exporter APIs appear in generated code
- generated source uses only its controlled using block or fully qualified names and does not rely on consumer usings
- a package-consumer test verifies that the packed `Light.PortableResults.Validation.OpenApi` package brings the generator along as an analyzer asset and that the analyzer loads without runtime assembly reference failures

Snapshot tests are appropriate for generated source (`Verify.SourceGenerators` is the recommended harness). The tests must also compile generated output and assert the resulting OpenAPI document so the generator cannot drift from the transformer contract. A baseline test for the sample compares the source-generated document against the previously manually configured one and asserts non-regression on schema/code/`oneOf` content; the response-level examples added by source generation are an expected addition over the baseline, not a difference the test should reject.

### Documentation and Sample

Update the README and the `NativeAotMovieRating` sample to show the intended user-facing workflow:

```csharp
[GeneratePortableValidationOpenApi]
public sealed partial class NewMovieRatingValidator : Validator<NewMovieRatingDto>
{
    // existing PerformValidation implementation, now using IsInRange(1, 5)
}

app.MapPut("/api/moviesRatings", AddMovieRating)
   .ProducesPortableValidationProblemFor<NewMovieRatingValidator>(
        configure: builder => builder.UseFormat(ValidationProblemSerializationFormat.Rich));
```

Concrete sample changes required:

- `NewMovieRatingValidator` becomes `partial`, gains `[GeneratePortableValidationOpenApi]`, and updates the rating check from `IsInBetween(1, 5)` to `IsInRange(1, 5)`.
- `NewMovieRatingEndpoint` removes its manual `WithErrorCodes(...)` and `WithInRangeError<int>()` calls and replaces them with `ProducesPortableValidationProblemFor<NewMovieRatingValidator>(...)`.
- The Scalar UI (`/docs`) and Swagger UI (`/swagger/index.html`) endpoints both render the source-generated document with concrete response-level example bodies that include `lowerBoundary: 1`, `upperBoundary: 5`, the length-range bounds, and the envelope's `category: "Validation"`. A short README section calls out the visible difference and notes any remaining rendering divergence between the two UIs (Scalar and Swashbuckle's UI handle OpenAPI 3.x examples slightly differently; pin which behavior is canonical).
- A document-equivalence test in the test suite (not in the sample) compares the generated document against the pre-source-gen baseline so the swap is provably non-breaking.

The docs should clearly explain the supported subset and the escape hatches. The most important messages are: source generation is precise when validation rules are explicit and annotated; arbitrary delegates and imperative custom validation require explicit documentation hints; automatic source-null errors emitted before `PerformValidation` are not generated in v1; and the analysis only sees top-level statements in the validation method body.

### Scope Boundaries

This plan does not attempt to infer arbitrary C# semantics or execute validators at build time. It does not analyze checks nested inside control-flow constructs; those are skipped with a warning diagnostic. It does not replace the manual `ProducesPortableValidationProblem(...)` builder APIs; generated contracts compose with them. It does not require every validator in an application to opt in. It does not change runtime validation behavior or the wire format.

Out of scope for this iteration, intentionally deferred to keep the first generator landing small:

- **`AsyncValidator` and `PerformValidationAsync`** — async validators produce a diagnostic when marked.
- **MVC integration** — Minimal APIs only; the generated static contract is reusable from MVC in a follow-up plan.
- **`ErrorOverrides`** — chains that override the default error code or metadata are treated as opaque.
- **Automatic source-null validation** — source-null errors emitted by the validator base class before `PerformValidation` runs are not inferred. Use explicit `IsNotNull()` rules or manual endpoint configuration when these errors should appear in OpenAPI.
- **Complex target-string inference** — chained member access, indexers, and computed targets are out of scope. The simple `context.Check(dto.Property)` case is supported in v1.
- **Child-validator composition** — validators invoking nested validators.
- **Collection-item validation.**

Each of these can be added without breaking the contracts established in this plan. The current plan deliberately keeps the analysis surface small so the source-generation infrastructure lands cleanly before scope expands.
