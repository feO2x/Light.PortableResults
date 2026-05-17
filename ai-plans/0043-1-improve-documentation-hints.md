# Improve Validation OpenAPI Documentation Hints

## Rationale

The first validation OpenAPI source generator can infer metadata from top-level annotated check chains, and it already offers `[PortableValidationOpenApiErrorHint]` for validation flows that are opaque to static analysis. The current hint is intentionally small: it can document an extra error code and optionally a metadata type. That is not enough for guarded rules, `Custom(...)`, `Must(...)`, `ErrorOverrides`, and other imperative validation paths where users still know the exact OpenAPI contract they want to publish.

This plan improves the explicit documentation hint model before broadening automatic control-flow analysis. The goal is to let users precisely document non-inferable validation errors while keeping the generator deterministic, NativeAOT-safe, and source-generator-friendly.

## Acceptance Criteria

- [ ] The validation OpenAPI hint API can document an error code with optional metadata schema information, optional response-example target, and optional response-example metadata values without requiring the generator to interpret arbitrary runtime objects.
- [ ] Existing `[PortableValidationOpenApiErrorHint]` usages remain source-compatible. If new attributes are introduced instead of extending the existing attribute, the old attribute continues to work for code-only and metadata-type hints.
- [ ] Hints can be applied at validator class level and `PerformValidation` method level, matching the current hint scope.
- [ ] Generated code emits the same builder calls that users would write manually: `WithErrorCodes(...)`, `WithErrorMetadata<T>(...)` or equivalent inline metadata schema configuration, and `WithErrorExample(...)` when example data is supplied.
- [ ] The generator validates malformed hints with clear diagnostics when the emitted endpoint schema would be ambiguous or impossible to generate.
- [ ] The generator keeps exhaustive schemas honest: hints add documented error contracts, and `AllowUnknownErrorCodes()` is still emitted only when the user explicitly requests it.
- [ ] The implementation remains incremental-generator-friendly. All hint model values flowing through the pipeline use value equality, deterministic ordering, and source-generator-safe attribute argument shapes.
- [ ] Generated source compiles in consumer projects with implicit usings disabled and nullable enabled, and continues to use fully qualified names or deterministic generated using directives.
- [ ] Automated tests cover code-only hints, metadata-type hints, inline metadata-schema hints, example target hints, example metadata hints, duplicate compatible hints, malformed hints, interaction with opaque-flow diagnostics, and generated OpenAPI document output.
- [ ] Documentation is updated to explain when to use explicit hints instead of `AllowUnknownErrorCodes()`, how hints compose with generated inference, and which validation flows still require manual endpoint builder configuration.

## Technical Details

### Scope

This plan is about explicit documentation hints only. It should not add branch-sensitive or branch-insensitive analysis for `if`, `switch`, loops, lambdas, local functions, or helper methods. Existing nested-check warnings remain useful because they tell the user when a hint may be needed.

The design should favor a small attribute model over a broad generator configuration system. General endpoint customization should continue to happen through the existing `configure` callback on `ProducesPortableValidationProblemFor<TValidator>(...)`.

### Attribute Model

Keep all source-generator-facing hint attributes in `Light.PortableResults.Validation.OpenApi`. Attribute constructor arguments and settable properties must use compiler-supported attribute types only: primitive values, strings, `Type`, enums, and arrays of those values.

The existing attribute should continue to work:

```csharp
[PortableValidationOpenApiErrorHint("MovieAlreadyRated")]
[PortableValidationOpenApiErrorHint("MovieAlreadyRated", typeof(MovieAlreadyRatedMetadata))]
```

To support richer hints, prefer extending `PortableValidationOpenApiErrorHintAttribute` with optional named properties. The hint model is still small enough that a single attribute is easier to discover, document, and reason about than a family of companion attributes. For example:

```csharp
[PortableValidationOpenApiErrorHint(
    "MovieAlreadyRated",
    typeof(MovieAlreadyRatedMetadata),
    ExampleTarget = "movieId")]

[PortableValidationOpenApiErrorHint(
    "RatingTooLow",
    ExampleTarget = "rating",
    ExampleMetadataKey = "minimum",
    ExampleMetadataInt64Value = 1)]
```

The exact property names can be refined during implementation. The important constraint is that schema documentation and example documentation are both possible without parsing object initializers or invoking runtime code.

Do not introduce companion attributes for code, schema, or example target configuration unless the implementation shows that the single-attribute model is structurally insufficient. The main case where a companion attribute may still be justified is repeated keyed example metadata for the same error example. If that is needed, use one focused companion attribute for repeated metadata entries rather than parallel arrays on the main hint attribute.

For example metadata values, support the same small constant family that the current rule metadata path can emit safely: string, integral numeric values, boolean, and `Type` as a string value. Decimal and floating-point values may be added if doing so fits cleanly with the existing emitter literal support. Complex example values are out of scope; users can still add those manually in the endpoint `configure` callback.

### Schema Hints

Code-only hints should continue to emit `builder.WithErrorCodes("Code")`.

Metadata-type hints should continue to emit `builder.WithErrorMetadata<TMetadata>("Code")`.

If inline metadata schema hints are added, they should follow the same conceptual model as `[ValidationErrorMetadataContract]`: a code has a set of required metadata properties, and each property has a CLR type. The generator should emit `builder.WithErrorMetadata("Code", _ => new OpenApiSchema { ... })` using `PortableOpenApiSchemaTypeMapper.Map<T>()`, matching the existing generated code for annotated custom rules.

Inline metadata schema hints are lower priority than code-only hints, metadata-type hints, example targets, and simple example metadata. If the implementation needs to stage the work, complete the simpler hint forms first and add inline schema hints only after the conflict and diagnostic rules are clear.

Do not instantiate validation error definitions or metadata objects. The generator should read attributes from symbols and emit builder calls directly.

### Example Hints

Example hints should be independent from schema hints. A user may document only the schema, only an example, or both. If an example supplies metadata values, the generated `WithErrorExample(...)` call should include the constant dictionary. If an example supplies no metadata values, it should emit the simpler target-only overload.

Hints contribute entries to the single generated validation response example that the current OpenAPI bridge composes for an endpoint response. This plan does not introduce multiple named OpenAPI examples per response.

The generator should not require every metadata schema property to appear in an example. OpenAPI examples are illustrative, not schema definitions. However, if the same hinted example supplies the same metadata key with incompatible values or types, the generator should report a diagnostic instead of choosing one arbitrarily.

### Conflict Rules

The generator should deduplicate compatible hints by error code and schema shape. Duplicate code-only hints are harmless. Duplicate metadata-type hints for the same code are compatible only when the metadata type is the same. Inline schema hints for the same code are compatible only when they declare the same metadata keys with the same CLR types.

Conflicts that would produce ambiguous schema configuration should be errors. Conflicts that only duplicate an example should either be deduplicated deterministically or reported as warnings if deduplication would hide user intent.

Hints should compose with inferred rules. If an inferred rule and a hint document the same code with the same schema shape, the generated schema should contain one contract. If they document the same code with incompatible schema shapes, the generator should report a diagnostic because the endpoint contract would be ambiguous.

### Diagnostics

Add diagnostics for malformed hint usage. The generator should treat hints as a small declarative contract and validate them before emitting code.

Code validation:

- Error codes must not be null, empty, or whitespace.
- Error codes should be emitted exactly as supplied. The generator should not trim, case-fold, or otherwise normalize them because the runtime error code contract is string-based.
- Duplicate code-only hints are allowed.
- A code-only hint is compatible with inferred or hinted metadata for the same code because it does not define a competing metadata shape.

Metadata schema validation:

- Metadata type hints must not use `typeof(void)`, open generic types, pointer types, by-reference types, function pointer types, or unresolved error symbols.
- The same code cannot be documented with two different CLR metadata types.
- The same code cannot be documented with both a CLR metadata type and an incompatible inline metadata schema.
- Inline metadata property keys must not be null, empty, or whitespace.
- Inline metadata property keys must be unique per code.
- Duplicate inline metadata properties are compatible only when both the key and CLR type match.
- Inline metadata property types should be representable by `PortableOpenApiSchemaTypeMapper`. Unsupported complex types may fall back to an unconstrained `OpenApiSchema` with a warning, but conflicts or unresolved types should be errors.
- Inline schema hints for the same code are compatible only when they declare the same metadata property set with the same CLR types.

Example validation:

- Example hint codes must pass the same code validation as schema hints.
- Example targets may be omitted, but supplied targets must not be empty or whitespace.
- Example metadata keys must not be null, empty, or whitespace.
- Example metadata values must specify exactly one supported constant value source.
- Example metadata values must not specify multiple constant sources at the same time, such as both `ConstantStringValue` and `ConstantInt64Value`.
- Example metadata values must specify at least one supported constant source.
- Duplicate example metadata keys for the same code and same example are compatible only when the emitted value and inferred CLR type are identical.
- Example metadata does not need to cover every schema property.
- Example metadata keys that are not present in a documented metadata schema for the same code should produce a warning, not an error, because examples are illustrative and the schema may be supplied manually in the endpoint `configure` callback.
- When an example metadata key matches a documented schema property, the constant value type should be assignable to, or reasonably compatible with, the schema property type. Incompatible values should produce a diagnostic.

Conflict validation:

- Hints and inferred rules for the same code must agree on metadata shape. Compatible duplicates are deduplicated; incompatible shapes are errors.
- Hints and inferred typed helper rules must not produce conflicting typed contracts for the same code.
- Class-level and method-level hints for the same contract are compatible duplicates.
- Class-level and method-level hints that document the same code with incompatible metadata contracts are errors, preferably reported at the more specific conflicting attribute.
- Multiple examples for the same code and target should either be deduplicated when identical or diagnosed when they differ. The generator should not silently choose one example arbitrarily.

Attribute placement validation:

- The generator only consumes hint attributes from marked validators and the analyzed `PerformValidation` method.
- Hint attributes on a marked validator's other methods should be ignored unless the implementation can confidently report a useful diagnostic without noisy false positives.
- Hint attributes on validators that are not marked with `[GeneratePortableValidationOpenApi]` do not need diagnostics from this generator.

Generated-code safety validation:

- Every hinted type name must be emitted fully qualified.
- Every hinted literal must be safe to emit as C# source.
- Hints must not require generated code to use reflection, instantiate arbitrary user types, or read runtime state.

Diagnostics should point at the attribute syntax when possible. Opaque-flow diagnostics should remain warnings; adding a hint is one supported way to satisfy the documentation gap, but the generator should not try to prove that a hint fully covers the opaque runtime flow.

### Generated Output

The emitter should keep deterministic ordering:

- code-only hints grouped and sorted by code;
- metadata schema hints sorted by code and metadata key;
- examples emitted in source order when that better reflects user intent, or sorted deterministically if source order is not preserved by the model.

Generated source must keep the existing controlled using block and must not depend on consumer implicit usings, global usings, aliases, or local using directives.

### Documentation And Examples

Update the README source-generation section with examples for:

- adding an error code for `Custom(...)`;
- documenting metadata for an opaque custom path;
- adding a response example target and metadata values;
- choosing between explicit hints and `AllowUnknownErrorCodes()`.

The documentation should state the intended rule clearly: use explicit hints when the endpoint emits known validation error contracts that the generator cannot infer; use `AllowUnknownErrorCodes()` when the endpoint may emit additional codes that are not enumerable at build time.
