# Improve Validation OpenAPI Documentation Hints

## Rationale

The first validation OpenAPI source generator (plan 0043-0) introduced a minimal `[PortableValidationOpenApiErrorHint]` that can document an extra error code with an optional metadata type. That is not enough for guarded rules, `Custom(...)`, `Must(...)`, `ErrorOverrides`, and other imperative paths where users still know exactly which contracts the endpoint publishes. This plan extends the hint model so users can declare schema *and* response-example content for non-inferable validation errors while keeping the generator deterministic, NativeAOT-safe, and incremental-pipeline-friendly. The change is purely additive: every existing hint usage keeps compiling unchanged, nothing is obsoleted, and no migration step is required.

## Acceptance Criteria

- [ ] The validation OpenAPI hint API can document an error code with optional metadata schema information, optional response-example target, and optional response-example metadata values without requiring the generator to interpret arbitrary runtime objects.
- [ ] Existing `[PortableValidationOpenApiErrorHint]` usages remain source-compatible for code-only and metadata-type hints.
- [ ] Hints can be applied at validator class level and `PerformValidation` method level. Both placements feed the same hint pipeline and are subject to the same conflict rules.
- [ ] Generated code emits the same builder calls a user would write manually: `WithErrorCodes(...)`, `WithErrorMetadata<T>(...)` or an inline metadata schema configuration, and `WithErrorExample(...)` when example data is supplied.
- [ ] Hints compose with inferred rules: matching schema shapes are deduplicated; conflicting shapes produce diagnostics; hints never silently weaken an exhaustive schema or trigger `AllowUnknownErrorCodes()` on their own.
- [ ] The implementation remains incremental-generator-friendly: all hint model values flowing through the pipeline use value equality, deterministic ordering, and source-generator-safe attribute argument shapes.
- [ ] Generated source compiles in consumer projects with implicit usings disabled and nullable enabled, and continues to use the existing controlled using block or fully qualified names.
- [ ] Automated tests are written.
- [ ] Documentation is updated to explain when to use explicit hints instead of `AllowUnknownErrorCodes()`, how hints compose with generated inference, and which flows still require the endpoint `configure` callback.

## Technical Details

### Scope

This plan covers explicit documentation hints only. It does not add branch-sensitive or branch-insensitive analysis for `if`, `switch`, loops, lambdas, local functions, or helper methods. The existing nested-check warning from 0043-0 remains useful because it tells the user when a hint is needed. General endpoint customization continues to happen through the existing `configure` callback on `ProducesPortableValidationProblemFor<TValidator>(...)`.

Conceptually, hint attributes are the user-facing counterpart to the generator-facing `[ValidationRule]` / `[ValidationRuleMetadata]` attributes from 0043-0: same model (a code, a metadata shape, optional constant values), applied at the validator instead of the check method. Reusing that mental model keeps the public surface coherent and lets the emitter share infrastructure for code grouping, schema deduplication, and example composition.

### Attribute Model

All hint attributes live in `Light.PortableResults.Validation.OpenApi`. Attribute constructor arguments and settable properties use compiler-supported types only: primitive values, strings, `Type`, enums, and arrays of those.

Schema hints and example hints are split into two attributes because they describe orthogonal concerns. A user may document only a schema, only an example, or both:

- **`[PortableValidationOpenApiErrorHint]`** — declares "this code is part of the endpoint's contract" and optionally its metadata shape. The existing single-arg and `(code, typeof(TMetadata))` forms continue to work; a new optional property accepts an inline metadata-property set via a paired companion attribute (see below) when neither a code-only nor a typed-metadata hint fits.
- **`[PortableValidationOpenApiExampleHint]`** — declares one entry in the validator-level response example for a given code, with an optional target and zero or more metadata key/value pairs. This attribute does not declare a schema. As a deliberate ergonomic shortcut, if a code appears only in an example hint, the generator treats the schema as `code-only` exactly as if the user had written `[PortableValidationOpenApiErrorHint("Code")]`. This removes a redundant declaration for the common opaque-`Custom(...)` case where the user wants to document the code and ship a sample body in a single place.

Because attribute properties cannot hold arbitrary key/value collections, repeated metadata entries are modeled with a small companion attribute keyed by the parent example's code. Multi-key examples — the headline case for built-in codes like `InRange` whose example needs both `lowerBoundary` and `upperBoundary` — are first-class, not a fallback:

```csharp
[PortableValidationOpenApiErrorHint("RatingTooLow")]

[PortableValidationOpenApiExampleHint("RatingTooLow", Target = "rating")]
[PortableValidationOpenApiExampleMetadata("RatingTooLow", "lowerBoundary", 1)]
[PortableValidationOpenApiExampleMetadata("RatingTooLow", "upperBoundary", 5)]

[PortableValidationOpenApiErrorHint("MovieAlreadyRated", typeof(MovieAlreadyRatedMetadata))]
[PortableValidationOpenApiExampleHint("MovieAlreadyRated", Target = "movieId")]
```

The companion's `code` argument matches the parent example hint's code; this keeps repeated metadata declarations associative without parallel arrays on the main attribute or any object initializer interpretation. To keep the association unambiguous, **a given scope (validator class or `PerformValidation` method) declares at most one example hint per code**. Examples are illustrative, and one canonical body per code per validator is sufficient; users who need multiple targets for the same code on one endpoint can supply them through the `configure` callback. Under that constraint, every `[PortableValidationOpenApiExampleMetadata]` matches exactly one example hint by `(code, scope)`; a companion attribute whose code does not match any example hint in its scope is an error. The implementer should pick whatever overload set on `PortableValidationOpenApiExampleMetadataAttribute` covers string, integral, boolean, and `Type`-as-string constants ergonomically; an `object` parameter is acceptable as a single overload if Roslyn round-trips the constant cleanly, but typed overloads are preferred because they preserve the intended literal shape (e.g. `1L` vs `1`) for the emitter. Decimal and floating-point values may be added if they fit cleanly with existing emitter literal support. Complex values stay out of scope and remain a `configure`-callback concern.

Inline metadata schema hints, when present, also use a small companion attribute (`[PortableValidationOpenApiErrorMetadataProperty(code, key, typeof(T))]`) for the same reason. They are lower priority than the other hint forms; if staging is needed, ship code-only hints, typed-metadata hints, and example hints first.

### Generated Output

The emitter reuses the existing infrastructure built for inferred rules: code-only hints are folded into existing `WithErrorCodes(...)` groupings; typed-metadata hints emit `WithErrorMetadata<TMetadata>(...)`; inline schema hints emit `WithErrorMetadata("code", _ => new OpenApiSchema { ... })` through `PortableOpenApiSchemaTypeMapper.Map<T>()`. Example hints emit `WithErrorExample(...)` calls — the simpler target-only overload when no metadata is supplied, and the dictionary overload otherwise.

Ordering is deterministic: schema calls grouped and sorted by code; metadata properties sorted by key; example entries emitted in a stable order derived from source position or a sort on `(code, target)`. Generated source continues to use the controlled using block from 0043-0 and must not rely on consumer implicit usings, global usings, aliases, or local using directives.

The generator does not instantiate validation error definitions or metadata objects at any point; everything flows from attribute symbols straight to builder calls, preserving the NativeAOT-safety guarantee from 0043-0.

### Hint Scope and Placement

Hint attributes are consumed only from validators marked with `[GeneratePortableValidationOpenApi]` and from their `PerformValidation` method. Class-level and method-level hints feed the same pipeline; the distinction is documentation locality and diagnostic anchoring, not semantics. Hints on other members of a marked validator are ignored without diagnostic to avoid noisy false positives. Hints on unmarked validators are not analyzed. Base-class hints are not chased, matching the analysis boundary established in 0043-0.

### Composition with `AllowUnknownErrorCodes`

Hints and `AllowUnknownErrorCodes()` are complementary, not alternatives. A validator may declare hints for the codes the endpoint *does* publish and additionally opt into `AllowUnknownErrorCodes()` for codes that are not enumerable at build time. The generator emits `AllowUnknownErrorCodes()` only when the user explicitly requested it via the existing opt-in attribute; declaring hints never triggers it implicitly.

### Diagnostics

The diagnostic philosophy mirrors 0043-0:

- **Errors** for malformed hints (empty/whitespace codes or keys, `typeof(void)` and other unrepresentable metadata types, unresolved error symbols), for conflicting contracts that would make the emitted schema ambiguous (same code with two different metadata types, same code with metadata-type and incompatible inline-schema, class-level and method-level hints declaring incompatible shapes for the same code, inferred-rule vs. hint disagreement on metadata shape), for example-hint structural violations (more than one example hint per code per scope), and for orphan companion attributes — any `[PortableValidationOpenApiExampleMetadata]` or `[PortableValidationOpenApiErrorMetadataProperty]` whose code does not match a parent hint in its scope.
- **Warnings** for illustrative-only mismatches that do not corrupt the contract: an example metadata key that does not appear in any documented schema for that code; an inline metadata property using a complex type that falls back to an unconstrained `OpenApiSchema`.
- **No diagnostic** for compatible duplicates — same code with identical metadata shape, identical example bodies — which are deduplicated silently. Deduplication is by `(code, schema shape)` for schema hints and by `(code, target, metadata)` for example hints.

Existing opaque-flow warnings from 0043-0 remain warnings; adding a hint is one supported way to satisfy the documentation gap, but the generator does not attempt to prove a hint fully covers the runtime flow.

Diagnostic IDs continue in the `LPRSG` range and point at the most specific attribute syntax available — the conflicting attribute, not the validator declaration.

### Tests

In addition to the standard generator/snapshot coverage already established by 0043-0, the hint-specific test surface should at minimum exercise: code-only, metadata-type, and inline-schema hints; example hints with and without metadata; multi-key example metadata; class-level vs. method-level placement; duplicate-compatible deduplication; each error and warning diagnostic; interaction with inferred rules for the same code; interaction with `AllowUnknownErrorCodes()`; and end-to-end OpenAPI document output for a sample validator that mixes inferred rules with hinted opaque calls.

### Documentation and Examples

Update the README source-generation section with worked examples for: adding an error code for `Custom(...)`; documenting metadata for an opaque custom path; adding a response-example target and metadata values, including a multi-key example; choosing between explicit hints and `AllowUnknownErrorCodes()`. The guiding rule to state plainly: use explicit hints when the endpoint emits known validation error contracts the generator cannot infer; use `AllowUnknownErrorCodes()` when the endpoint may emit additional codes that are not enumerable at build time. The two compose.
