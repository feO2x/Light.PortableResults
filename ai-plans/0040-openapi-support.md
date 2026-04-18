# OpenAPI Support for Portable Problem Details

## Rationale

Light.PortableResults can already serialize failure responses as RFC 9457 Problem Details over HTTP, but there is no corresponding way to document those failure shapes for OpenAPI. Callers building Minimal APIs or MVC endpoints on top of `ToMinimalApiResult` and `ToMvcActionResult` have no library-provided helpers to tell OpenAPI generators which problem schema an endpoint produces for validation errors or for other failure codes such as 401, 404, or 409.

This plan closes that gap by adding schema-only CLR types and explicit endpoint metadata helpers for failure responses. The implementation stays intentionally static: OpenAPI generators need only a CLR type to infer a schema, while the runtime HTTP writers continue to serialize failures dynamically from `Errors`, `ProblemDetailsInfo`, and `PortableResultsHttpWriteOptions`. No automatic OpenAPI inference, runtime schema transformers, or dedicated OpenAPI integration package is introduced.

The plan also revisits the success-side OpenAPI naming and usage model. A successful `Result<T>` has only two meaningful body contracts: either the body is just `TValue`, or the body is `{ value, metadata }`. If the body is just `TValue`, callers should use the normal ASP.NET Core OpenAPI metadata helpers such as `Produces<TValue>(...)` or `ProducesResponseTypeAttribute<TValue>`. If the body includes `metadata`, then the metadata type is part of the contract and must be explicit. Because of that, the Light.PortableResults-specific success-side OpenAPI surface should only exist for the wrapped `{ value, metadata }` case and must always require `TMetadata`.

Introducing the failure-side types also creates an opportunity to establish a consistent naming scheme across the whole OpenAPI surface. The existing success-side types (`WrappedResponse<TValue, TMetadata>`, `ProducesPortableResult(...)`, `ProducesPortableResultAttribute<...>`) carry generic names that do not clearly identify them as schema-only OpenAPI helpers. This plan renames them to `PortableSuccessResponse<TValue, TMetadata>`, `ProducesPortableSuccessResponse<TValue, TMetadata>`, and `ProducesPortableSuccessResponseAttribute<TValue, TMetadata>`. Since the library is still pre-1.0, the breaking rename is acceptable now.

## Acceptance Criteria

- [ ] `WrappedResponse<TValue, TMetadata>` is renamed to `PortableSuccessResponse<TValue, TMetadata>`, the existing success-side helpers are renamed to `ProducesPortableSuccessResponse<TValue, TMetadata>` and `ProducesPortableSuccessResponseAttribute<TValue, TMetadata>`, and the single-generic success-side helpers are removed. After this change, `WrappedResponse`, `ProducesPortableResult<TValue>`, `ProducesPortableResultAttribute<TValue>`, and any other success-side `object`-based OpenAPI convenience surface no longer exist anywhere in the codebase.
- [ ] The success-side OpenAPI model is explicit: callers who serialize only `TValue` in successful responses use the standard ASP.NET Core OpenAPI metadata APIs, while callers who serialize `{ value, metadata }` use the Light.PortableResults-specific success helper with an explicit `TMetadata`.
- [ ] `Light.PortableResults.AspNetCore.Shared` contains the exact schema-only OpenAPI types defined in this plan: `PortableSuccessResponse<TValue, TMetadata>`, `PortableError`, `PortableError<TMetadata>`, `PortableValidationErrorDetail`, `PortableValidationErrorDetail<TMetadata>`, `PortableProblemDetails<TErrorMetadata, TProblemMetadata>`, `PortableProblemDetails`, `PortableRichValidationProblemDetails<TErrorMetadata, TProblemMetadata>`, `PortableRichValidationProblemDetails`, `PortableAspNetCoreValidationProblemDetails<TErrorDetailMetadata, TProblemMetadata>`, and `PortableAspNetCoreValidationProblemDetails`.
- [ ] `Light.PortableResults.AspNetCore.MinimalApis` contains the exact `RouteHandlerBuilder` extension method overloads defined in this plan, including their status-code defaults and content-type defaults. There are no one-generic middle-tier overloads for success or failure responses.
- [ ] `Light.PortableResults.AspNetCore.Mvc` contains the exact response metadata attributes defined in this plan, including their status-code defaults and content-type defaults. There are no one-generic middle-tier attributes for success or failure responses.
- [ ] The generic parameter meanings are fixed as follows throughout the public API: `TMetadata` on `PortableSuccessResponse<TValue, TMetadata>` is success-body metadata, `TErrorMetadata` is metadata on each rich error item, `TErrorDetailMetadata` is metadata on each ASP.NET Core-compatible `errorDetails` item, and `TProblemMetadata` is top-level problem metadata.
- [ ] Rich problem helpers and ASP.NET Core-compatible validation problem helpers are separate public APIs so callers must choose the schema that matches their configured HTTP serialization format.
- [ ] The implementation does not change the runtime HTTP serialization behavior of `LightResult`, `LightResult<T>`, `LightActionResult`, `LightActionResult<T>`, or the JSON writers in `Light.PortableResults`.
- [ ] Automated tests are written for the renamed success helpers and for all new Minimal API and MVC OpenAPI metadata helpers. The MVC test project gains a new unit test class for attribute metadata.
- [ ] `README.md` documents the breaking rename, the new OpenAPI support for error responses, and the success-side rule that plain `TValue` success responses use standard ASP.NET Core OpenAPI helpers while `{ value, metadata }` success responses use `ProducesPortableSuccessResponse<TValue, TMetadata>` or `ProducesPortableSuccessResponseAttribute<TValue, TMetadata>`.

## Technical Details

### Public Schema Types

Add the following schema-only types to `Light.PortableResults.AspNetCore.Shared`. These types exist only for OpenAPI schema generation and are not used by the runtime HTTP writing pipeline.

All schema-only model types in this plan should be public and not sealed. They are OpenAPI contract types, are not instantiated by the library at runtime, and may be useful to callers as reusable contract models.

The failure-side non-generic and two-generic variants form a two-tier hierarchy. Convenience non-generic subtypes inherit from the two-generic base with `object` substituted for both type parameters. The two-generic failure-side base types must therefore not be sealed. `PortableSuccessResponse<TValue, TMetadata>` does not need a non-generic or single-generic subtype because the success-side OpenAPI surface should only be used when the metadata type is explicit.

`PortableSuccessResponse<TValue, TMetadata>` is only used to document wrapped success bodies that contain both `value` and `metadata`. Plain `TValue` success bodies are documented with the standard ASP.NET Core OpenAPI metadata APIs.

**Success response:**
- `PortableSuccessResponse<TValue, TMetadata>` with the properties `TValue Value` and `TMetadata Metadata`.

**Error items:**
- `PortableError` with the properties `string Message`, `string? Code`, `string? Target`, `ErrorCategory Category`, and `object? Metadata`.
- `PortableError<TMetadata>` with the properties `string Message`, `string? Code`, `string? Target`, `ErrorCategory Category`, and `TMetadata Metadata`.
- `PortableValidationErrorDetail` with the properties `string Target`, `int Index`, `string? Code`, `ErrorCategory? Category`, and `object? Metadata`. The `Index` property is the zero-based position of the corresponding error message within the `errors[target]` array for the same target.
- `PortableValidationErrorDetail<TMetadata>` with the properties `string Target`, `int Index`, `string? Code`, `ErrorCategory? Category`, and `TMetadata Metadata`.

**Problem details (generic + non-generic):**
- `PortableProblemDetails<TErrorMetadata, TProblemMetadata>` deriving from `ProblemDetails` with the properties `IReadOnlyList<PortableError<TErrorMetadata>> Errors` and `TProblemMetadata Metadata`.
- `PortableProblemDetails` inheriting from `PortableProblemDetails<object, object>`.
- `PortableRichValidationProblemDetails<TErrorMetadata, TProblemMetadata>` deriving from `ProblemDetails` with the properties `IReadOnlyList<PortableError<TErrorMetadata>> Errors` and `TProblemMetadata Metadata`.
- `PortableRichValidationProblemDetails` inheriting from `PortableRichValidationProblemDetails<object, object>`.
- `PortableAspNetCoreValidationProblemDetails<TErrorDetailMetadata, TProblemMetadata>` deriving from `HttpValidationProblemDetails` with the properties `IReadOnlyList<PortableValidationErrorDetail<TErrorDetailMetadata>>? ErrorDetails` and `TProblemMetadata Metadata`.
- `PortableAspNetCoreValidationProblemDetails` inheriting from `PortableAspNetCoreValidationProblemDetails<object, object>`.

`PortableRichValidationProblemDetails` is structurally identical to `PortableProblemDetails`, but the separation is intentional: keeping them as distinct CLR types causes OpenAPI generators to emit different schema definitions with meaningful names for general errors and validation errors. Do not collapse the two families into one type.

The success-response rename should be applied consistently across the code base, tests, XML documentation, and README.

### Minimal API Helpers

Rename the existing success helper in `PortableResultsEndpointExtensions` to the following exact signature:

- `public static RouteHandlerBuilder ProducesPortableSuccessResponse<TValue, TMetadata>(this RouteHandlerBuilder builder, int statusCode = StatusCodes.Status200OK, string contentType = "application/json")`

Remove the single-generic success helper. Callers who do not serialize metadata in the success response body should use the standard ASP.NET Core OpenAPI metadata APIs such as `Produces<TValue>(...)`. Callers who do serialize metadata in the body should use `ProducesPortableSuccessResponse<TValue, TMetadata>(...)`.

Add the following exact failure-response helper signatures to `PortableResultsEndpointExtensions`:

- `public static RouteHandlerBuilder ProducesPortableProblem(this RouteHandlerBuilder builder, int statusCode = StatusCodes.Status500InternalServerError, string contentType = "application/problem+json")`
- `public static RouteHandlerBuilder ProducesPortableProblem<TErrorMetadata, TProblemMetadata>(this RouteHandlerBuilder builder, int statusCode = StatusCodes.Status500InternalServerError, string contentType = "application/problem+json")`
- `public static RouteHandlerBuilder ProducesPortableRichValidationProblem(this RouteHandlerBuilder builder, int statusCode = StatusCodes.Status400BadRequest, string contentType = "application/problem+json")`
- `public static RouteHandlerBuilder ProducesPortableRichValidationProblem<TErrorMetadata, TProblemMetadata>(this RouteHandlerBuilder builder, int statusCode = StatusCodes.Status400BadRequest, string contentType = "application/problem+json")`
- `public static RouteHandlerBuilder ProducesPortableAspNetCoreValidationProblem(this RouteHandlerBuilder builder, int statusCode = StatusCodes.Status400BadRequest, string contentType = "application/problem+json")`
- `public static RouteHandlerBuilder ProducesPortableAspNetCoreValidationProblem<TErrorDetailMetadata, TProblemMetadata>(this RouteHandlerBuilder builder, int statusCode = StatusCodes.Status400BadRequest, string contentType = "application/problem+json")`

The non-generic overloads point to `PortableProblemDetails`, `PortableRichValidationProblemDetails`, and `PortableAspNetCoreValidationProblemDetails`. The two-generic overloads point to the corresponding `<TErrorMetadata, TProblemMetadata>` or `<TErrorDetailMetadata, TProblemMetadata>` types. There are no one-generic middle-tier overloads; callers who want only typed problem metadata use the two-generic overload with `object` as the first type argument.

The validation helpers default to `400 Bad Request` because that is the default validation response in the library today, but the `statusCode` parameter must also allow callers to document `422 Unprocessable Content` when they intentionally expose that contract.

`ProducesPortableProblem` is also the correct helper for non-validation 4xx responses such as `401 Unauthorized`, `403 Forbidden`, and `404 Not Found`; callers simply pass the relevant status code. There are no dedicated per-status-code helpers for these cases.

Keep the API explicit rather than accepting `ValidationProblemSerializationFormat` as a method parameter. OpenAPI must point to one concrete schema, and explicit helper names make it much harder for callers to document the wrong shape.

### MVC Attributes

Rename the existing success attribute to the following exact name and type shape:

- `public sealed class ProducesPortableSuccessResponseAttribute<TValue, TMetadata> : ProducesResponseTypeAttribute<PortableSuccessResponse<TValue, TMetadata>>`

The success attribute should keep the constructor signature `public ... (int statusCode = StatusCodes.Status200OK, string contentType = "application/json")`.

Remove the single-generic success attribute. Callers who do not serialize metadata in the success response body should use the standard ASP.NET Core response metadata attributes such as `ProducesResponseTypeAttribute<TValue>`. Callers who do serialize metadata in the body should use `ProducesPortableSuccessResponseAttribute<TValue, TMetadata>`.

Add the following exact failure-response attribute families:

- `ProducesPortableProblemAttribute`
- `ProducesPortableProblemAttribute<TErrorMetadata, TProblemMetadata>`
- `ProducesPortableRichValidationProblemAttribute`
- `ProducesPortableRichValidationProblemAttribute<TErrorMetadata, TProblemMetadata>`
- `ProducesPortableAspNetCoreValidationProblemAttribute`
- `ProducesPortableAspNetCoreValidationProblemAttribute<TErrorDetailMetadata, TProblemMetadata>`

Each attribute should derive from `ProducesResponseTypeAttribute<TSchema>` with the corresponding schema-only type from `Light.PortableResults.AspNetCore.Shared`. There are no one-generic middle-tier attributes.

The exact constructor signatures should be:

- problem attributes: `public ... (int statusCode = StatusCodes.Status500InternalServerError, string contentType = "application/problem+json")`
- rich validation attributes: `public ... (int statusCode = StatusCodes.Status400BadRequest, string contentType = "application/problem+json")`
- ASP.NET Core-compatible validation attributes: `public ... (int statusCode = StatusCodes.Status400BadRequest, string contentType = "application/problem+json")`

As with the Minimal API helpers, the validation attributes must allow callers to override the status code to `422` when needed.

### Scope Boundaries

This feature should remain documentation-only:

- do not change the runtime JSON serialization code in `Light.PortableResults`
- do not modify `LightResult<T>` or `LightActionResult<T>` to infer response metadata automatically
- do not introduce `Microsoft.AspNetCore.OpenApi` transformers or a separate OpenAPI integration package
- do not attempt to infer validation schema shape from `PortableResultsHttpWriteOptions`

Instead, the caller explicitly chooses the documented schema that matches the endpoint contract and the configured `ValidationProblemSerializationFormat`.

### Automated Tests

Add unit tests in the Minimal API and MVC test projects that verify the exact renamed and newly added helper surfaces from this plan.

The MinimalApis test project already has `PortableResultsEndpointExtensionsTests` as a template. The MVC test project currently has no unit test class for attribute metadata; add one following the same pattern.

The test coverage should include:

- the renamed success-side Minimal API helper registers `PortableSuccessResponse<TValue, TMetadata>` metadata entries
- the renamed success-side MVC attribute points to `PortableSuccessResponse<TValue, TMetadata>`
- `ProducesPortableProblem...` and `ProducesPortableProblemAttribute...` register the correct `PortableProblemDetails...` schema types
- `ProducesPortableRichValidationProblem...` and `ProducesPortableRichValidationProblemAttribute...` register the correct `PortableRichValidationProblemDetails...` schema types
- `ProducesPortableAspNetCoreValidationProblem...` and `ProducesPortableAspNetCoreValidationProblemAttribute...` register the correct `PortableAspNetCoreValidationProblemDetails...` schema types
- non-generic and two-generic overloads both point to the expected schema-only CLR types
- the removed single-generic success-side helpers are no longer present
- default content types and default status codes match the signatures defined in this plan

### README Documentation

Extend `README.md` in the HTTP / ASP.NET Core section rather than creating a separate documentation chapter.

The README changes should include:

- a note about the breaking rename: `WrappedResponse<TValue, TMetadata>` becomes `PortableSuccessResponse<TValue, TMetadata>`, `ProducesPortableResult<TValue, TMetadata>` becomes `ProducesPortableSuccessResponse<TValue, TMetadata>`, and `ProducesPortableResultAttribute<TValue, TMetadata>` becomes `ProducesPortableSuccessResponseAttribute<TValue, TMetadata>`
- a short explanation of the success-side rule: use standard ASP.NET Core OpenAPI helpers when the success body is just `TValue`, and use `ProducesPortableSuccessResponse<TValue, TMetadata>` or `ProducesPortableSuccessResponseAttribute<TValue, TMetadata>` only when the success body is `{ value, metadata }`
- two Minimal APIs examples that each combine `ProducesPortableSuccessResponse<TValue, TMetadata>(...)` with `ProducesPortableProblem(...)` and a validation helper; one example should use `ProducesPortableRichValidationProblem(...)` and the other should use `ProducesPortableAspNetCoreValidationProblem(...)`
- an MVC example that combines `ProducesPortableSuccessResponseAttribute<TValue, TMetadata>` with one problem attribute and one validation attribute
- a concise explanation of the two validation problem formats: `AspNetCoreCompatible` documents `errors` as `Dictionary<string, string[]>` plus an optional `errorDetails` array, while `Rich` documents `errors` as an array of Light.PortableResults-style error objects
- an explanation of the `Index` property on `PortableValidationErrorDetail`: it is the zero-based position of the corresponding error message within the `errors[target]` array for the same target, allowing `errorDetails` entries to be correlated back to the matching message
- a note that callers must choose the OpenAPI helper that matches the actual configured `ValidationProblemSerializationFormat`
- a note that the typed metadata CLR types are schema-only helpers for OpenAPI; the runtime still serializes `MetadataObject`, so callers are responsible for keeping the documented schema aligned with the metadata they actually produce

Keep the README focused on practical endpoint examples and avoid going deep into internal implementation details.
