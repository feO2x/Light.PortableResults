# Exhaustive Error-Code Schemas and Flattened Envelopes

## Rationale

Plans `0040-1`, `0040-2`, and `0040-4` produce a working OpenAPI surface, but two design choices weaken the generated document for downstream consumers (Swagger UI, Scalar, Kiota, NSwag, openapi-generator):

1. **Documented error codes are not exhaustive by default.** `CreateDocumentedErrorItemSchemaAsync` in `PortableResultsOpenApiDocumentTransformer` always appends a `$ref` to the canonical `PortableError` / `PortableValidationErrorDetail` schema as a fallback branch and narrows with `anyOf` instead of `oneOf`. The fallback exists so undocumented codes still validate, but it has three undesirable side effects: (a) the discriminator is never load-bearing because the fallback also matches, (b) client generators that key off discriminated unions cannot produce sealed type hierarchies, and (c) endpoints that genuinely declare their full error contract via `WithErrorCodes(...)` / `With*Error<T>()` are documented as if they could still emit anything. Pre-stable is the right moment to flip the default to exhaustive and offer a small explicit opt-out.

2. **The derived envelope is an `allOf` composition rather than a concrete object schema.** `CreateErrorResponseSchemaAsync` emits `allOf: [ $ref(canonical), extensionWithErrorsAndMetadataOverrides ]`. Several mainstream codegen tools (NSwag, openapi-generator) handle multi-level `allOf` chains awkwardly, producing intermediate base classes that do not reflect the real wire shape. Flattening *the outer envelope only* — copying the canonical properties and overriding `errors` / `errorDetails` / `metadata` — produces a schema that renders cleanly in Swagger UI / Scalar and codegens to a single concrete type per response slot.

This plan does **not** flatten the per-code error variants (`PortableError__InRange`, etc.). The `allOf [base, { code: const, metadata: $ref }]` shape is the canonical JSON Schema idiom for discriminated narrowing; mainstream tools render it well, and Kiota in particular relies on the structural relationship for discriminator subtyping. Flattening per-code variants would also multiply duplication (~25 built-in codes × every endpoint that opts in) without a corresponding rendering win.

OpenAPI support has not shipped, so this is the right time to make these changes as a coordinated, breaking schema-design correction. Validator-driven endpoint generation, source generators, and example synthesis are explicitly out of scope.

## Acceptance Criteria

- [ ] The transformer's documented-error item schema is exhaustive by default. When `CreateDocumentedErrorItemSchemaAsync` produces a discriminated item schema, it emits `oneOf` (not `anyOf`) over the documented variants and does **not** append a fallback `$ref` to the canonical `PortableError` / `PortableValidationErrorDetail` schema. The discriminator mapping is unchanged.
- [ ] An opt-in escape hatch `AllowUnknownErrorCodes()` is added to both `PortableProblemOpenApiBuilder` and `PortableValidationProblemOpenApiBuilder`. When the flag is set on the response attribute, the transformer reverts to the previous behavior: `anyOf` over the documented variants plus the canonical fallback `$ref`. The discriminator is preserved in both modes; in the non-exhaustive mode the documented mapping still narrows the documented codes and the fallback covers the rest.
- [ ] A boolean `bool AllowUnknownErrorCodes { get; set; }` property is added to `PortableOpenApiErrorResponseAttributeBase`. It is the single source of truth that the transformer reads, so MVC consumers can set it directly on `[ProducesPortableProblem]` / `[ProducesPortableValidationProblem]` and Minimal API consumers go through the builder helper. The default value is `false` (exhaustive).
- [ ] In exhaustive mode the narrowed item schema requires a non-null `code`: the wrapper-level `Required` set continues to include `"code"`, every per-code variant requires `"code"` (already the behavior of `CreateCodeSpecificSchema`), and the schema is asserted to be honest about this contract via a regression test. Documenting an endpoint with `WithErrorCodes(...)` is therefore a developer-asserted guarantee that every emitted error item carries a `code` and that code is in the documented set; if either half can be violated, the correct response is `AllowUnknownErrorCodes()`.
- [ ] When `WithErrorCodes` / inline `WithErrorMetadata` / `With*Error<T>()` are not called *and* `AllowUnknownErrorCodes()` is also not called, the response continues to reference the canonical envelope component (no narrowed item schema is synthesized). This preserves the current behavior of an undecorated `ProducesPortableProblem(...)` and means exhaustive-by-default only applies once the endpoint has documented at least one code.
- [ ] The derived error response envelope is flattened. `CreateErrorResponseSchemaAsync` produces a concrete `OpenApiSchema` with `Type = JsonSchemaType.Object`, the canonical properties copied directly, and `errors` / `errorDetails` / `metadata` overridden with the narrowed shapes. The result no longer uses `AllOf` to compose against the canonical base. The component id naming (`<Canonical>__<Op>__<Status>__<ContentType>`) is unchanged.
- [ ] Per-code error item variants (`PortableError__<Code>`, `PortableValidationErrorDetail__<Code>`, and their inline endpoint-scoped counterparts) continue to use `allOf [base, { code: const, metadata: $ref }]`. `CreateCodeSpecificSchema` is not changed by this plan, and tests assert the per-code variant shape is preserved verbatim.
- [ ] The success response envelope synthesized by `CreateSuccessResponseSchemaAsync` is unchanged structurally: it already produces a concrete object schema with `value` and (optionally) `metadata`, so no flattening work is needed there.
- [ ] `PortableResultsOpenApiSchemas` exposes exactly two public helpers that return *fresh* property dictionaries for the canonical error envelopes: `CreatePortableProblemDetailsProperties(OpenApiDocument document)` (used for both `PortableProblemDetails` and `PortableRichValidationProblemDetails`, which currently share a property set) and `CreatePortableAspNetCoreValidationProblemDetailsProperties(OpenApiDocument document)`. These helpers replace the existing private `CreateProblemDetailsProperties` and are the single source of truth used by both `InstallInto` (for the canonical components) and the transformer (for the flattened derived envelopes), so the canonical and derived schemas can never structurally drift.
- [ ] Both modes preserve discriminator mapping coverage. In exhaustive mode the `discriminator.mapping` keys are exactly the documented raw codes. In non-exhaustive mode the mapping is unchanged from today (documented codes only; the fallback branch carries no mapping entry). Documented mapping values continue to use JSON-Pointer-escaped `$ref`s as established in `0040-1`.
- [ ] The `NativeAotMovieRating` sample is updated to the new exhaustive-by-default behavior. Endpoints that already enumerate their full error contract gain no new code; endpoints (if any) that genuinely emit unknown codes call `.AllowUnknownErrorCodes()` so the produced document remains accurate.
- [ ] Automated tests cover:
    - The exhaustive default: `WithErrorCodes(...)` produces a `oneOf` with no fallback `$ref`, and the discriminator mapping enumerates exactly the documented codes.
    - Inline-only narrowing in exhaustive mode: an endpoint that calls only `.WithInRangeError<int>()` (no `WithErrorCodes`) produces a single-branch `oneOf` with the inline endpoint-scoped variant and no fallback `$ref`.
    - Metadata-only narrowing: an endpoint that calls only `.WithMetadata<T>()` (no error narrowing, no `AllowUnknownErrorCodes()`) produces a flattened envelope with `metadata` overridden by the narrowed reference and `errors` / `errorDetails` still pointing at the canonical item schema.
    - The opt-out: `WithErrorCodes(...).AllowUnknownErrorCodes()` produces the previous `anyOf + fallback` shape.
    - Mixed global + inline narrowing in exhaustive mode (`WithErrorCodes(...).WithInRangeError<int>()`) continues to produce one `oneOf` branch per documented code with no fallback.
    - The undecorated case (no `WithErrorCodes`, no inline narrowing, no `AllowUnknownErrorCodes()`) still yields a plain `$ref` to the canonical envelope, not a degenerate `oneOf` over an empty set.
    - The narrowed-item `code`-required contract: the wrapper-level `Required` set contains `"code"` and every per-code variant requires `"code"` in both exhaustive and non-exhaustive modes.
    - The flattened outer envelope: the derived response component is a concrete object schema with the canonical properties copied verbatim, `errors` / `errorDetails` overridden with the narrowed array, and `metadata` overridden when `WithMetadata<T>()` is used. `AllOf` is absent at the outer envelope level.
    - The per-code error variants (`PortableError__<Code>`, inline endpoint-scoped variants) still use `allOf [base, extension]` — explicit regression coverage so the non-flattening of per-code variants cannot be silently changed.
    - A full document-validation pass continues to produce a spec-valid OpenAPI 3.0 and 3.1 document under the new schemas.
- [ ] `README.md` is updated to describe the exhaustive-by-default behavior of `WithErrorCodes` / `With*Error<T>()`, the `AllowUnknownErrorCodes()` opt-out, the contract that documented endpoints assert every emitted error carries a known `code`, and the flattened derived envelope shape. Any prose that previously described the fallback `$ref` as inevitable is removed or qualified.

## Technical Details

### Exhaustive-by-Default Discriminated Union

The change is local to `CreateDocumentedErrorItemSchemaAsync` in `PortableResultsOpenApiDocumentTransformer`. After collecting `documentedVariants`, the method currently appends a fallback `$ref` to `itemBaseSchemaId` and emits `anyOf` over (documented variants + fallback). The new logic reads `attribute.AllowUnknownErrorCodes`:

- **Exhaustive (default).** Emit `OneOf = documentedVariants.Select(v => v.SchemaReference).ToList()`, no fallback. The discriminator mapping is unchanged. `oneOf` is semantically correct here because each narrowed variant is an `allOf` restriction of the base schema, and without the base in the union no two branches can both match for a given concrete error item — the `code` `const` (or `enum` for OpenAPI 3.0) ensures exactly one branch matches.

- **Non-exhaustive (opt-in).** Emit the existing shape: `anyOf` over (documented variants + fallback `$ref` to `itemBaseSchemaId`). `oneOf` is *not* correct here because the fallback `$ref` to the canonical base also matches the documented codes (every narrowed variant is an `allOf` restriction of the base), so two branches would match and `oneOf` validation would fail. The discriminator mapping carries only the documented codes, as today.

The "no documented variants and no opt-out" branch (the early `return null;` path that today produces an unwrapped `$ref` to the canonical envelope) is preserved verbatim. Exhaustive-by-default only applies once at least one code has been documented.

#### The Code-Required Contract

Exhaustive `oneOf` correctness depends on every emitted error item carrying a non-null `code`. The narrowed item schema enforces this at validation time on two levels: the wrapper-level `Required` set contains `"code"` (already true today via `CreateDocumentedErrorItemSchemaAsync`), and every per-code variant requires `"code"` (already true via `CreateCodeSpecificSchema`). The canonical `PortableError` / `PortableValidationErrorDetail` schemas leave `code` nullable and not required so that the *un-narrowed* envelope can still describe error items that omit a code; once an endpoint opts into narrowing, the narrowed schema tightens the contract.

This means calling `WithErrorCodes(...)` is a developer-asserted contract that every error this endpoint emits carries a `code` and that code is in the documented set. If either half can be violated — code-less errors, third-party error propagation, defensive `Error.Internal(...)` paths whose codes are not enumerable up-front — the correct response is `AllowUnknownErrorCodes()`. The plan does not modify the runtime to enforce code presence on errors emitted from documented endpoints; that would be a separate `Light.PortableResults` runtime change and is out of scope here.

### `AllowUnknownErrorCodes()` Builder Method

Both `PortableProblemOpenApiBuilder` and `PortableValidationProblemOpenApiBuilder` gain a one-line method that flips the new attribute property to `true`. The method returns `this` for chaining and is idempotent. Because the property lives on `PortableOpenApiErrorResponseAttributeBase`, MVC consumers can set it via attribute property syntax (`[ProducesPortableProblem(StatusCode = 400, AllowUnknownErrorCodes = true)]`) without going through the builders.

### Flattened Outer Envelope

`CreateErrorResponseSchemaAsync` currently builds:

```
new OpenApiSchema {
  AllOf = [ CreateSchemaReference(document, canonicalSchemaId), extensionSchema ]
}
```

where `extensionSchema` carries the optional `metadata` override and the `errors`/`errorDetails` array. The new shape is a single concrete object schema:

```
new OpenApiSchema {
  Type = JsonSchemaType.Object,
  Properties = <fresh copy of canonical properties, with errors/errorDetails/metadata overridden>,
  Required = <fresh copy of canonical required set>
}
```

To avoid duplicating the canonical property definitions, `PortableResultsOpenApiSchemas` exposes exactly two new public helpers that return fresh property dictionaries: `CreatePortableProblemDetailsProperties(OpenApiDocument document)` and `CreatePortableAspNetCoreValidationProblemDetailsProperties(OpenApiDocument document)`. The first is used by both `PortableProblemDetails` and `PortableRichValidationProblemDetails` because they currently share an identical property set; the second is used by `PortableAspNetCoreValidationProblemDetails`, which carries the additional `errorDetails` slot. The existing private `CreateProblemDetailsProperties` is removed in favor of these. `InstallInto` calls the same helpers when authoring the canonical components, so canonical and derived schemas are guaranteed to stay structurally aligned. The transformer dispatches on the canonical schema id resolved by `ResolveCanonicalErrorEnvelopeSchemaId` to pick the right helper.

The transformer's flatten path:

1. Resolves the canonical schema id (`ResolveCanonicalErrorEnvelopeSchemaId`, unchanged).
2. Calls the matching property-dictionary helper to obtain a fresh `Dictionary<string, IOpenApiSchema>` keyed ordinally.
3. If `attribute.TopLevelMetadataType is not null`, replaces `properties["metadata"]` with the narrowed metadata reference (built the same way as today via `GetStableSchemaReferenceAsync` under the existing metadata-component naming).
4. If `documentedErrorSchema` is not null, replaces `properties["errors"]` (or `properties["errorDetails"]` for the ASP.NET-Core-compatible format) with the narrowed array. The branch picking `errors` vs `errorDetails` is identical to today.
5. Builds the flattened concrete schema and registers it under the existing `derivedEnvelopeSchemaId`.

The `errors`-vs-`errorDetails` selector remains keyed on the canonical schema id, so the `PortableAspNetCoreValidationProblemDetails` envelope continues to use `errorDetails` while both rich envelopes use `errors`.

### Per-Code Variants Stay as `allOf`

`CreateCodeSpecificSchema` is intentionally untouched. It continues to produce:

```
allOf:
  - $ref: '#/components/schemas/PortableError'
  - { type: object, properties: { code: const | enum, metadata: $ref? }, required: [code] }
```

This is the canonical JSON Schema idiom for discriminated narrowing and is consumed correctly by Swagger UI, Scalar, and Kiota's discriminator subtype detection. A regression test asserts the shape so a future contributor cannot quietly flatten it.

### Scope Boundaries

- This plan does not change the canonical schema catalog (`PortableError`, `PortableValidationErrorDetail`, `PortableProblemDetails`, `PortableRichValidationProblemDetails`, `PortableAspNetCoreValidationProblemDetails`, `ErrorCategory`). Their public component ids and shapes are preserved.
- This plan does not change the per-code error variant shape (`PortableError__<Code>`, `PortableValidationErrorDetail__<Code>`, inline endpoint-scoped variants).
- This plan does not introduce validator-driven endpoint generation, source-generator code paths, automatic example emission, or any new automation that infers documented error codes from validator implementations. Those remain candidates for separate, larger plans.
- This plan does not unify exhaustive/non-exhaustive into a single shape. The opt-out exists precisely because some endpoints genuinely emit unknown codes (third-party error propagation, defensive `Error.Internal(...)` paths) and weakening the discriminator only for those endpoints is the honest documentation choice.
- This plan does not modify the `Light.PortableResults` runtime to enforce that errors emitted from documented endpoints carry a non-null `code`. Exhaustive mode is a developer-asserted contract enforced at JSON Schema validation time on the consumer side, not at runtime by the producer. Tightening the runtime side is a separate plan.
- This plan does not touch runtime serialization. The wire format produced by `LightResult`, `LightActionResult`, and the JSON writers is unchanged; only the generated OpenAPI document is affected.
