# 0040 Plan Deviations

This document compares the original OpenAPI plan in `ai-plans/0040-0-openapi-support.md` with the implementation direction that was ultimately taken across `0040-1` through `0040-5`.

## Summary

The original plan treated OpenAPI support as a thin, schema-only CLR layer on top of the existing ASP.NET Core integrations. The final implementation went in a different direction: OpenAPI became its own opt-in package, schema generation moved from surrogate CLR types to a document transformer plus a library-authored schema catalog, validation error contracts became code-driven and registry-based, and later follow-up plans tightened the design for package boundaries, NativeAOT, coverage, and downstream OpenAPI tooling behavior.

The most important architectural change is that OpenAPI is no longer modeled primarily through public CLR response types such as `PortableProblemDetails<TErrorMetadata, TProblemMetadata>` or `PortableSuccessResponse<TValue, TMetadata>`. Instead, the library now owns the OpenAPI document directly and synthesizes response schemas from endpoint metadata.

## Major Deviations From The Original Plan

### 1. OpenAPI moved out of the runtime ASP.NET Core packages into dedicated opt-in packages

**Original plan:**
`Light.PortableResults.AspNetCore.Shared` would contain the schema-only OpenAPI CLR types, `Light.PortableResults.AspNetCore.MinimalApis` would expose the `RouteHandlerBuilder` helpers, and `Light.PortableResults.AspNetCore.Mvc` would expose the response metadata attributes. No separate OpenAPI package was introduced.

**Implemented direction:**
OpenAPI support was moved into a new dedicated package, `Light.PortableResults.AspNetCore.OpenApi`, with its own service-registration entry point, `AddPortableResultsOpenApi()`. The runtime packages `Light.PortableResults.AspNetCore.MinimalApis` and `Light.PortableResults.AspNetCore.Mvc` no longer expose the OpenAPI helper or attribute surface at all. A second bridge package, `Light.PortableResults.Validation.OpenApi`, was later added for validation-specific built-in error contracts. The redesign also explicitly targets `Microsoft.AspNetCore.OpenApi`; Swashbuckle / NSwag-specific integration is not part of the public surface.

**Impact:**
This is a major packaging and layering deviation. The final design keeps the runtime packages free of the `Microsoft.AspNetCore.OpenApi` dependency and makes OpenAPI support an explicit opt-in concern instead of part of the core ASP.NET Core integration surface.

### 2. The schema-only CLR surrogate model was abandoned entirely

**Original plan:**
The public OpenAPI model was centered around schema-only CLR types such as:

- `PortableSuccessResponse<TValue, TMetadata>`
- `PortableError` and `PortableError<TMetadata>`
- `PortableValidationErrorDetail` and `PortableValidationErrorDetail<TMetadata>`
- `PortableProblemDetails<TErrorMetadata, TProblemMetadata>`
- `PortableRichValidationProblemDetails<TErrorMetadata, TProblemMetadata>`
- `PortableAspNetCoreValidationProblemDetails<TErrorDetailMetadata, TProblemMetadata>`

OpenAPI generators were expected to infer schemas from those CLR types.

**Implemented direction:**
That entire surrogate model was removed. The library now authors canonical OpenAPI schemas directly through `PortableResultsOpenApiSchemas` and uses `PortableResultsOpenApiDocumentTransformer` to install canonical components and synthesize operation-specific derived schemas.

**Impact:**
This is the core architectural pivot. It avoids generic CLR type names leaking into OpenAPI component ids, removes the need for alias hierarchies and naming workarounds, and stops promising metadata CLR shapes that the runtime HTTP writers do not actually enforce.

### 3. The success-response design changed from a metadata-generic CLR wrapper to a mode-aware single-generic helper

**Original plan:**
The success-side OpenAPI helper existed only for the wrapped `{ value, metadata }` body shape and always required an explicit metadata type through `PortableSuccessResponse<TValue, TMetadata>`, `ProducesPortableSuccessResponse<TValue, TMetadata>`, and `ProducesPortableSuccessResponseAttribute<TValue, TMetadata>`. Plain `TValue` success responses were supposed to use standard ASP.NET Core OpenAPI APIs.

**Implemented direction:**
The final design collapsed the public success helper to `ProducesPortableSuccessResponse<TValue>` and `ProducesPortableSuccessResponseAttribute<TValue>`. The generated success schema is now selected from the effective `MetadataSerializationMode`: under `ErrorsOnly` it documents the bare `TValue` response shape, and under `Always` it synthesizes a wrapped `{ value, metadata }` envelope. Top-level metadata can still be narrowed explicitly, but it is no longer a public generic parameter on the helper surface.

**Impact:**
This is both an API-shape deviation and a behavioral one. The success-side OpenAPI surface is now mode-aware and can follow the application default from `PortableResultsHttpWriteOptions`, which is more dynamic than the strictly static, metadata-generic model described in `0040-0`. The transient rename from `WrappedResponse<TValue, TMetadata>` to `PortableSuccessResponse<TValue, TMetadata>` became a short-lived intermediate state rather than the final contract.

### 4. Separate validation helper families were collapsed into one validation helper with format selection

**Original plan:**
Minimal APIs and MVC would expose separate helper/attribute families for:

- general problems
- rich validation problems
- ASP.NET Core-compatible validation problems

The split was intentional so callers had to choose the exact validation schema shape explicitly.

**Implemented direction:**
The final public surface exposes only:

- `ProducesPortableProblem`
- `ProducesPortableValidationProblem`
- `ProducesPortableProblemAttribute`
- `ProducesPortableValidationProblemAttribute`

The effective validation schema is resolved from `PortableResultsHttpWriteOptions.ValidationProblemSerializationFormat` or a per-endpoint/per-attribute override. The MVC attributes are no longer `ProducesResponseTypeAttribute<TSchema>` wrappers; they are custom endpoint metadata attributes consumed directly by the OpenAPI document transformer.

**Impact:**
This is a real API simplification relative to the original plan. Instead of encoding the validation format in the helper name, the final design keeps one validation helper and lets the transformer choose the canonical validation schema based on the effective format.

### 5. Metadata typing moved from public generic parameters to explicit schema narrowing and a contract registry

**Original plan:**
Metadata typing was expressed directly in public generic parameters such as `TErrorMetadata`, `TErrorDetailMetadata`, and `TProblemMetadata`. The documented contract for metadata was therefore tied to CLR generic arguments on the public API.

**Implemented direction:**
The final design treats metadata slots as open objects by default and narrows them explicitly only when the caller opts in. Top-level metadata narrowing is attached through endpoint metadata. Per-error-code metadata narrowing is driven through `ConfigureErrorMetadataContracts(...)`, `PortableErrorMetadataContractsBuilder`, `IPortableErrorMetadataContractRegistry`, and inline `WithErrorMetadata(...)` overrides.

This later expanded again in `0040-2`, where contracts were widened from "CLR type only" to a closed discriminated union:

- CLR type contracts
- schema-factory contracts
- explicit no-metadata contracts

**Impact:**
This is a substantial conceptual deviation. The OpenAPI layer no longer assumes that one public CLR generic argument can faithfully describe the runtime metadata shape. Instead, metadata documentation is selective, per-endpoint, and often per-error-code.

### 6. Error-code-specific contracts became a first-class part of the OpenAPI model

**Original plan:**
The plan documented only coarse response envelopes. It did not define a registry for specific error codes, code-discriminated unions, or endpoint-level narrowing of `errors[*].metadata` and `errorDetails[*].metadata`.

**Implemented direction:**
The final design introduced error-code-aware OpenAPI generation. Endpoints can declare documented codes through `WithErrorCodes(...)`, register global metadata contracts in DI, and add inline per-endpoint metadata contracts for specific codes. The transformer emits per-code schema variants, discriminator mappings, and narrowed response envelopes.

`0040-2` then went further by adding `ValidationErrorCodes`, `BuiltInValidationErrorContracts`, and `RegisterBuiltInValidationErrors()` so the built-in validation taxonomy is available as a reusable OpenAPI contract catalog instead of requiring every consumer to redeclare it.

**Impact:**
This is not just a deviation but a major expansion beyond the original plan. The final OpenAPI surface documents individual error-code contracts rather than only top-level problem-envelope shapes.

### 7. Validation-specific OpenAPI support became a bridge package with built-in catalogs and typed helpers

**Original plan:**
Validation support was limited to documenting one of two validation problem envelope shapes through the shared schema-only CLR types.

**Implemented direction:**
`0040-2` introduced `Light.PortableResults.Validation.OpenApi` as a dedicated bridge package. That package owns:

- the built-in validation error contract catalog
- `RegisterBuiltInValidationErrors()`
- `ValidationErrorCodes`
- typed builder extensions such as `WithInRangeError<T>()`, `WithGreaterThanError<T>()`, and related helpers for site-specific narrowing of polymorphic built-in codes

The plan also renamed several validation error codes for clarity: `LengthIn` became `LengthInRange`, `Matches` became `Pattern`, `IsInBetween` became `InRange`, and `NotInBetween` became `NotInRange`.

**Impact:**
This is a broader validation/OpenAPI integration model than `0040-0` described. OpenAPI documentation for validation is now organized around a shared framework-owned code taxonomy plus optional endpoint-level narrowing.

### 8. NativeAOT forced another design pivot away from CLR surrogates used by typed validation helpers

**Original plan:**
The original plan did not center NativeAOT as a design constraint for OpenAPI schema generation.

**Implemented direction:**
`0040-4` found that the typed validation helper path still relied on CLR record surrogates flowing through the ASP.NET Core schema generator, which breaks in NativeAOT unless every generated type is in the application's `JsonSerializerContext`. The fix was to delete those helper-only CLR record surrogates and switch the typed validation helpers to schema-factory contracts instead. The public `PortableOpenApiSchemaTypeMapper` was added to map CLR primitive-like types to `OpenApiSchema`, and inline endpoint metadata now stores `PortableErrorMetadataContract` values rather than just `Type` values.

**Impact:**
This is another strong deviation from the CLR-surrogate mindset of `0040-0`. Even where the redesign had briefly kept CLR types for endpoint-scoped narrowing, the final direction removed them in favor of schema factories so the OpenAPI stack remains NativeAOT-compatible.

### 9. The final error-union model became exhaustive-by-default and the derived envelopes were flattened

**Original plan:**
The original plan did not describe per-error-code discriminated unions at all. It also assumed inheritance/composition through CLR schema types rather than transformer-authored flattened schemas.

**Implemented direction:**
`0040-5` tightened the document model again:

- documented error codes are exhaustive by default
- `AllowUnknownErrorCodes()` is the explicit opt-out for non-exhaustive endpoints
- narrowed item unions use `oneOf` without a fallback branch in exhaustive mode
- derived problem envelopes are flattened into concrete object schemas instead of outer `allOf` composition against the canonical envelope
- shared property/required helpers in `PortableResultsOpenApiSchemas` became the single source of truth for both canonical and derived envelopes

**Impact:**
The final document shape is considerably more precise than `0040-0` envisioned and is tuned for downstream tooling behavior in Swagger UI, Scalar, Kiota, NSwag, and openapi-generator. This is another area where the implementation went materially beyond the original plan rather than simply implementing it differently.

### 10. The testing strategy changed from package-local helper tests to package-scoped, document-generation-heavy coverage

**Original plan:**
The plan expected tests for the Minimal API helpers, MVC attributes, and the renamed success helpers inside the existing ASP.NET Core test projects, with a new MVC test class for attribute metadata.

**Implemented direction:**
The final design introduced dedicated package-oriented test coverage:

- `Light.PortableResults.AspNetCore.OpenApi.Tests`
- `Light.PortableResults.Validation.OpenApi.Tests`

`0040-3` explicitly reorganized the tests around those package boundaries, preferred sociable in-memory OpenAPI document-generation tests over isolated surface checks, and tracked coverage with `coverage.runsettings` so generated files do not distort the numbers.

**Impact:**
This is a practical deviation in delivery strategy. The tests now validate the transformer-driven package design end to end instead of primarily asserting helper registration behavior in the original runtime packages.

## Original Intent That Survived

Not everything changed. Two important parts of `0040-0` still describe the final design accurately:

- OpenAPI support remains documentation-only. The work did not change the runtime HTTP serialization behavior of `LightResult`, `LightResult<T>`, `LightActionResult`, `LightActionResult<T>`, or the JSON writers in `Light.PortableResults`.
- The caller still has to document the actual response shape deliberately. Even though the final implementation is more dynamic than the original plan, it still relies on explicit endpoint metadata, explicit error-code registration, and explicit opt-ins rather than trying to infer the complete OpenAPI contract automatically from runtime behavior.

## Net Result

The original plan was a CLR-type-centric OpenAPI layer embedded into the ASP.NET Core runtime packages. The implemented direction is a package-separated, transformer-driven, error-code-aware OpenAPI system with validation-specific bridge packages, NativeAOT-safe schema factories, and a more precise final schema model for downstream tooling.

In short: `0040-0` proposed "document PortableResults by exposing schema-only CLR response types." The final implementation became "generate an OpenAPI document directly from explicit endpoint metadata and library-owned schema building blocks."