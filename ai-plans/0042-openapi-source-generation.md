# Validator-Driven OpenAPI Source Generation

## Rationale

The current OpenAPI implementation is explicit and NativeAOT-safe: endpoints opt into validation error codes through `ProducesPortableValidationProblem(...)`, built-in validation contracts are registered through `Light.PortableResults.Validation.OpenApi`, and documented error codes are exhaustive by default with `AllowUnknownErrorCodes()` as the opt-out. This gives correct schemas, but it requires endpoint authors to duplicate information that already exists in their validators.

This plan introduces a source-generation path that lets users mark validators as OpenAPI-documentable. The generator inspects supported validation calls in `PerformValidation` / `PerformValidationAsync`, derives the same `PortableValidationProblemOpenApiBuilder` calls users write manually today, and emits reflection-free code that can be consumed by Minimal API endpoint metadata. The generator must be conservative: built-in and explicitly annotated validation rules are supported, while arbitrary delegates (`Must`) and imperative custom blocks (`Custom`) are treated as opaque unless the user supplies explicit documentation hints.

The goal is not to infer arbitrary C# behavior. The goal is to create a stable, AOT-compatible contract model where built-in checks and user-defined rule methods expose enough compile-time metadata for the generator to produce accurate OpenAPI response schemas.

## Acceptance Criteria

- [ ] A new source-generator project is added for validation OpenAPI generation. It targets `netstandard2.0`, is packaged as a Roslyn analyzer, and does not add runtime dependencies to applications beyond the existing validation/OpenAPI packages.
- [ ] Public generator-facing attributes are added without violating layering: validation-rule and validation-error-contract attributes live in the validation layer and do not reference `Microsoft.OpenApi`; OpenAPI-specific opt-in attributes live in `Light.PortableResults.Validation.OpenApi`.
- [ ] Built-in validation checks and built-in validation error definitions are annotated so the generator can identify their error code, metadata shape, and method-argument-to-metadata bindings without hard-coded method-name tables.
- [ ] A public static-abstract contract interface is added in `Light.PortableResults.Validation.OpenApi`, allowing generated partial validator classes to expose `ConfigurePortableValidationOpenApi(PortableValidationProblemOpenApiBuilder builder)` without reflection.
- [ ] A Minimal API helper such as `ProducesPortableValidationProblemFor<TValidator>(...)` is added. It calls the generated validator contract and then applies any caller-supplied manual builder configuration.
- [ ] Marking a validator with the OpenAPI generation attribute requires the validator class to be `partial`. The generator emits a clear diagnostic when the class is not partial or is otherwise unsupported.
- [ ] The generator supports synchronous `Validator<T>` / `Validator<TSource, TValidated>` and asynchronous `AsyncValidator<T>` / `AsyncValidator<TSource, TValidated>` validators by inspecting the corresponding `PerformValidation` or `PerformValidationAsync` method body.
- [ ] The generator recognizes direct `ValidationContext.Check(...)` calls and fluent chains of supported check extension methods. It supports both standalone calls and assignments that consume a `ValidatedValue<T>` returned by a check chain.
- [ ] The generator derives built-in no-metadata and fixed-metadata errors as `WithErrorCodes(...)` calls and derives built-in typed comparison/range errors as the existing typed helper calls such as `WithInRangeError<T>()`.
- [ ] The generator supports user-defined validation error definitions and user-defined check extension methods when they are explicitly annotated with generator-facing metadata that identifies the error code and metadata schema.
- [ ] `Must(...)`, `Predicate`, and delegate-based validation are treated as opaque unless the failing definition or wrapper method is statically documentable. Unsupported opaque flows produce diagnostics and require an explicit opt-in to unknown errors or explicit emitted-error hints.
- [ ] `Custom(...)` is treated as opaque by default because it can emit zero, one, or many errors. A documented validator that uses `Custom(...)` must either provide explicit emitted-error hints or opt into unknown errors.
- [ ] Generated code is NativeAOT-safe: it does not use reflection, does not instantiate arbitrary validation error definitions for documentation, and does not call ASP.NET Core's runtime schema exporter for generated validation metadata.
- [ ] The `NativeAotMovieRating` sample is updated to demonstrate the source-generated path: validators are partial and annotated, endpoints use the generated helper instead of manually repeating the built-in validation error codes, and the generated OpenAPI document is equivalent to the manually configured one.
- [ ] Automated tests are added for generator output, diagnostics, Minimal API integration, built-in rule coverage, user-defined rule support, opaque `Must` / `Custom` handling, and NativeAOT-safe generated code shape.
- [ ] Documentation is updated to explain the generator opt-in model, supported validation patterns, required annotations for custom rules, limitations around delegates and imperative custom validation, and how to mix generated and manual endpoint OpenAPI configuration.

## Technical Details

### Project Structure

Add a new analyzer/source-generator project, tentatively `Light.PortableResults.Validation.OpenApi.SourceGeneration`, under `src/`. It should target `netstandard2.0`, reference the Roslyn packages needed for an incremental generator, and be packed as an analyzer. The generator project may reference the validation and validation-OpenAPI assemblies for symbol names and attribute definitions, but generated application code must depend only on the runtime packages already used by the application.

Add a matching test project under `tests/`, for example `Light.PortableResults.Validation.OpenApi.SourceGeneration.Tests`. The tests should compile in-memory source snippets with the generator, assert diagnostics, inspect generated source, and run at least a few generated validators through the existing in-memory OpenAPI document test utilities.

The solution currently has no Roslyn source-generation infrastructure, so this plan should establish the package layout, test helpers, and central package versions for `Microsoft.CodeAnalysis.CSharp` / analyzer testing in a way that can be reused by future generators.

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
    sourceArgument: "lowerBoundary",
    valueTypeSource: ValidationMetadataValueTypeSource.Argument)]
[ValidationRuleMetadata(
    ValidationErrorMetadataKeys.UpperBoundary,
    sourceArgument: "upperBoundary",
    valueTypeSource: ValidationMetadataValueTypeSource.Argument)]
public static Check<T> IsInBetween<T>(
    this Check<T> check,
    T lowerBoundary,
    T upperBoundary,
    bool shortCircuitOnError = false)
```

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

The generated method should be deterministic, stable across builds, and idempotent with existing builder semantics. It should group simple built-in codes into as few `WithErrorCodes(...)` calls as practical and emit typed helper calls for typed comparison/range rules.

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

The generator should inspect the body of `PerformValidation` / `PerformValidationAsync` on marked validators and recognize common validation patterns:

- `context.Check(dto.Property).IsNotEmpty();`
- `dto.Comment = context.Check(dto.Comment).HasLengthIn(10, 1000);`
- fluent chains such as `context.Check(dto.Name).IsNotNullOrWhiteSpace().HasLengthIn(1, 100);`
- explicit target overloads when the target argument is statically understandable

The first implementation should intentionally stay conservative. It does not need to support arbitrary data flow, loops, complex local aliases, collection item validation, or validators invoked indirectly through helper methods unless those helper methods are themselves annotated as validation rules. Unsupported patterns should produce diagnostics that tell the user how to fix the documentation path: annotate a wrapper method, provide explicit emitted-error hints, or opt into unknown errors.

The generator does not need target information to produce response schemas. Target inference is useful for future generated examples, but schema generation can initially focus on error codes and metadata shapes. If target information is collected, it should be treated as best-effort and should not be required for a schema to be generated.

### Built-In Rule Coverage

Annotate the built-in check extension methods rather than hard-coding method names in the generator. This keeps the generator extensible and gives user-defined check methods the same path as built-ins.

Built-in rule annotations should cover:

- no-metadata codes: `NotNull`, `Null`, `NotEmpty`, `Empty`, `NotNullOrWhiteSpace`, `Email`, `DigitsOnly`, `LettersAndDigitsOnly`
- fixed metadata codes: `Count`, `MinCount`, `MaxCount`, `MinLength`, `MaxLength`, `LengthInRange`, `Pattern`, `Enum`, `EnumName`, `PrecisionScale`
- typed comparison/range codes: `EqualTo`, `NotEqualTo`, `GreaterThan`, `GreaterThanOrEqualTo`, `LessThan`, `LessThanOrEqualTo`, `InRange`, `NotInRange`, `ExclusiveRange`

For fixed metadata codes, generated endpoint configuration can use `WithErrorCodes(...)` because global registration via `RegisterBuiltInValidationErrors()` already supplies the metadata schema. For typed comparison/range codes, generated endpoint configuration should use the existing typed helper methods (`WithInRangeError<T>()`, etc.) because the endpoint pins down the concrete `T`.

### User-Defined Rules

User-defined rules should follow the same model as built-ins:

1. The error definition declares its stable code and metadata properties through attributes.
2. The check extension method declares which error definition it emits and how method arguments map to metadata properties.
3. The generator reads those attributes and emits either `WithErrorCodes(...)` when a registered global contract is enough, or an inline schema-factory metadata contract when endpoint-specific type narrowing is required.

Generated inline schema factories should be authored with `OpenApiSchema` construction and `PortableOpenApiSchemaTypeMapper`, not with reflection or `JsonSchemaExporter`. If a user-defined metadata property uses an unsupported complex type, the generator should either fall back to an unconstrained `OpenApiSchema` for that property with a diagnostic, or require the user to provide a custom schema hint attribute.

### `Must`, `Predicate`, and `Custom`

Delegate-based validation is not generally analyzable and must be treated as a boundary:

- `Must(predicate)` with no explicit definition remains opaque because the built-in `Predicate` code intentionally has no stable global metadata contract.
- `Must(predicate, definition)` is documentable only when the supplied definition is statically resolvable and its type has a validation error contract attribute.
- `Must(predicate, overrides)` is documentable only when the override code and metadata shape are explicitly declared through a documentation hint; arbitrary `MetadataObject` construction should not be interpreted.
- `Custom(...)` is opaque by default because the delegate can add zero, one, or many errors to the context.

Provide explicit opt-ins for opaque flows:

- A validator-level or method-level attribute that tells the generator to call `AllowUnknownErrorCodes()`.
- One or more emitted-error hint attributes that explicitly list codes and metadata shapes emitted by custom validation.
- The recommended pattern for reusable predicates is an annotated wrapper check method, e.g. `IsValidSlug(...)`, rather than raw `Must(...)` calls in validators.

Diagnostics should be helpful but not hostile. Opaque flows in a marked validator should produce warnings by default, and generated code should either omit those flows or call `AllowUnknownErrorCodes()` only when the user explicitly opted into that behavior. The generator should not silently weaken an exhaustive schema.

### NativeAOT and Performance

Generated code must be startup-only metadata code and must not affect the runtime validation hot path. The generator itself runs at build time; the emitted code runs only when endpoints are mapped and OpenAPI metadata is attached.

NativeAOT requirements:

- no reflection over validators or definitions at runtime
- no generated use of `Type.GetType`, constructor activation, or scanning assemblies
- no schema generation through `JsonSchemaExporter`
- no generated dependency on serializer metadata for generated validation metadata schemas
- generated schema factories create fresh `OpenApiSchema` instances to avoid mutable schema reuse

The source generator should be incremental and symbol-based. It should avoid whole-compilation scans where possible by filtering on the opt-in validator attribute and validation-rule attributes.

### Tests

Tests should cover both generator behavior and integrated OpenAPI output:

- generated code for a validator similar to `NewMovieRatingValidator`
- diagnostics for non-partial marked validators
- diagnostics for unsupported `Must` / `Custom` patterns
- built-in typed helper generation for `IsInBetween(1, 5)` and similar comparison/range checks
- fixed metadata built-ins such as `HasLengthIn(10, 1000)`
- user-defined check methods with annotated error definitions and metadata mappings
- explicit unknown-error opt-in producing `AllowUnknownErrorCodes()`
- generated Minimal API endpoint metadata producing the same `oneOf` / metadata schemas as manual builder calls
- a NativeAOT-oriented generated source assertion that no reflection/schema-exporter APIs appear in generated code

Snapshot tests are appropriate for generated source, but the tests should also compile generated output and assert the resulting OpenAPI document so the generator cannot drift from the transformer contract.

### Documentation and Sample

Update the README and the NativeAOT sample to show the intended user-facing workflow:

```csharp
[GeneratePortableValidationOpenApi]
public sealed partial class NewMovieRatingValidator : Validator<NewMovieRatingDto>
{
    // existing PerformValidation implementation
}

app.MapPut("/api/moviesRatings", AddMovieRating)
   .ProducesPortableValidationProblemFor<NewMovieRatingValidator>(
        configure: builder => builder.UseFormat(ValidationProblemSerializationFormat.Rich));
```

The docs should clearly explain the supported subset and the escape hatches. The most important message is that source generation is precise when validation rules are explicit and annotated, while arbitrary delegates and imperative custom validation require explicit documentation hints.

### Scope Boundaries

This plan does not attempt to infer arbitrary C# semantics or execute validators at build time. It does not generate examples yet, although the manifest model should leave room for examples later. It does not replace the manual `ProducesPortableValidationProblem(...)` builder APIs; generated contracts compose with them. It does not require every validator in an application to opt in. It does not change runtime validation behavior or the wire format.
