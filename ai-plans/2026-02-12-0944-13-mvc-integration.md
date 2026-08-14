# ASP.NET Core MVC Integration for Light.Results

## Rationale

Light.Results already integrates with ASP.NET Core Minimal APIs to produce success HTTP responses or RFC 9457 Problem Detail responses from `Result<T>` and `Result` instances. This plan extends the same capabilities to ASP.NET Core MVC by providing custom `IActionResult` implementations. The design mirrors the Minimal API integration as closely as possible: callers explicitly convert results via a `ToMvcActionResult` extension method, the same JSON converters and shared HTTP infrastructure are reused, and no MVC filters or conventions are introduced. Only JSON serialization is supported; XML and other formats are out of scope.

## Acceptance Criteria

- [ ] A new project `Light.Results.AspNetCore.Mvc` exists under `src/`, targeting .NET 10 with a `FrameworkReference` to `Microsoft.AspNetCore.App` and a `ProjectReference` to `Light.Results.AspNetCore.Shared`. Native AOT is **not** enabled (`IsAotCompatible` is omitted).
- [ ] `LightActionResult` and `LightActionResult<T>` implement `IActionResult` and correctly produce success or Problem Detail JSON responses, including status codes, content types, metadata headers, and response bodies — matching the behavior of the existing `LightResult` / `LightResult<T>` Minimal API types.
- [ ] Extension methods `ToMvcActionResult` (and `ToHttp201CreatedMvcActionResult`) on `Result` and `Result<T>` create the corresponding `LightActionResult` / `LightActionResult<T>` instances.
- [ ] A `Module` class provides `AddLightResultsForMvc` (an `IServiceCollection` extension) that registers `LightResultsHttpWriteOptions`, the HTTP header conversion service, and configures `Microsoft.AspNetCore.Mvc.JsonOptions` with the default Light.Results JSON converters.
- [ ] `IHttpResultEnricher` is supported: if registered in DI, results are enriched before header/body serialization, just as in the Minimal API integration.
- [ ] OpenAPI attributes `ProducesLightResultAttribute<TValue>` and `ProducesLightResultAttribute<TValue, TMetadata>` are provided for documenting MVC action success return types. Validation Problem Detail OpenAPI metadata (e.g., `ProducesValidationProblem`) is out of scope for this ticket and should be addressed separately — this applies to both MVC and Minimal APIs.
- [ ] `LightResultsHttpWriteOptions` is reused without changes — no MVC-specific options type is needed.
- [ ] Automated tests are written for the new MVC integration, covering both success and error scenarios.
- [ ] `WrappedResponse<TValue, TMetadata>` is moved from `Light.Results.AspNetCore.MinimalApis` to `Light.Results.AspNetCore.Shared`, and the Minimal API project's usings are updated accordingly.
- [ ] `src/AGENTS.md` is updated to include the new project in the overview.

## Technical Details

### Project Setup

Create `src/Light.Results.AspNetCore.Mvc/Light.Results.AspNetCore.Mvc.csproj`:
- Target framework is inherited from `Directory.Build.props` (net10.0).
- `FrameworkReference` to `Microsoft.AspNetCore.App`.
- `ProjectReference` to `Light.Results.AspNetCore.Shared`.
- Do **not** set `<IsAotCompatible>` — MVC itself is not AOT-compatible.

### Base Class: `BaseLightActionResult<TResult>`

Mirror `BaseLightResult<TResult>` from the Minimal APIs project. This abstract class:
- Implements `IActionResult`.
- Accepts the same constructor parameters: the result value, optional `HttpStatusCode? successStatusCode`, optional `string? location`, optional `LightResultsHttpWriteOptions? overrideOptions`, optional `JsonSerializerOptions? serializerOptions`.
- In `ExecuteResultAsync(ActionContext context)`:
  1. Resolve `IHttpResultEnricher` from `context.HttpContext.RequestServices` and enrich the result if the enricher is registered.
  2. Set headers by reusing `HttpExtensions.SetStatusCodeFromResult`, `SetContentTypeFromResult`, `SetMetadataValuesAsHeadersIfNecessary`, and setting the `Location` header if provided. Resolve `LightResultsHttpWriteOptions` via `HttpExtensions.ResolveLightResultOptions` and `IHttpHeaderConversionService` from DI.
  3. Delegate body writing to the abstract `WriteBodyAsync` method.
- Expose public read-only properties for `ReceivedResult`, `SuccessStatusCode`, `Location`, `OverrideOptions`, and `SerializerOptions` (same as `BaseLightResult<TResult>`).

### Concrete Types: `LightActionResult` and `LightActionResult<T>`

**`LightActionResult`** (sealed, extends `BaseLightActionResult<Result>`):
- `WriteBodyAsync` resolves `JsonSerializerOptions` from MVC's `Microsoft.AspNetCore.Mvc.JsonOptions` (via `IOptions<Microsoft.AspNetCore.Mvc.JsonOptions>`), looks up `JsonTypeInfo` for `Result`, and serializes using `Utf8JsonWriter` on `HttpContext.Response.BodyWriter` — same pattern as the Minimal API `LightResult`.

**`LightActionResult<T>`** (sealed, extends `BaseLightActionResult<Result<T>>`):
- Same approach but resolves `JsonTypeInfo` for `Result<T>`.

**JSON options resolution**: Create a small `MvcJsonDefaults` helper (analogous to `MinimalApis.Serialization.JsonDefaults`) that resolves `JsonSerializerOptions` from `IOptions<Microsoft.AspNetCore.Mvc.JsonOptions>` instead of `IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>`.

### Extension Methods: `MvcActionResultExtensions`

Static class with four methods:
- `ToMvcActionResult(this Result, ...)` → `LightActionResult`
- `ToMvcActionResult<T>(this Result<T>, ...)` → `LightActionResult<T>`
- `ToHttp201CreatedMvcActionResult(this Result, ...)` → `LightActionResult` with `HttpStatusCode.Created`
- `ToHttp201CreatedMvcActionResult<T>(this Result<T>, ...)` → `LightActionResult<T>` with `HttpStatusCode.Created`

All methods accept the same optional parameters as the Minimal API counterparts (`successStatusCode`/`location`/`overrideOptions`/`serializerOptions`), except for the `Created` variants which hardcode the status code.

### DI Registration: `Module`

`AddLightResultsForMvc(this IServiceCollection)`:
1. Calls `AddLightResultHttpWriteOptions()` (from `Light.Results.Http.Writing.Module`).
2. Calls `AddLightResultsHttpHeaderConversionService()` (from the same module).
3. Calls `ConfigureMvcJsonOptionsForLightResults()` (see below).

`ConfigureMvcJsonOptionsForLightResults(this IServiceCollection)`: Configures `Microsoft.AspNetCore.Mvc.JsonOptions` via `services.AddOptions<MvcJsonOptions>().Configure<LightResultsHttpWriteOptions>(...)` to add the default Light.Results JSON converters (`AddDefaultLightResultsHttpWriteJsonConverters`). Exposed as a separate public method (analogous to `ConfigureMinimalApiJsonOptionsForLightResults`) so callers who manage their own DI setup can configure just the JSON options without re-registering everything.

### OpenAPI Attributes

**`ProducesLightResultAttribute<TValue>`**: Derives from `ProducesResponseTypeAttribute` (or implements `IApiResponseMetadataProvider`), emitting `WrappedResponse<TValue, object>` as the response type schema. The `WrappedResponse` type from the Minimal APIs project should be moved to `Light.Results.AspNetCore.Shared` (or a new shared type should be created) so it can be reused.

**`ProducesLightResultAttribute<TValue, TMetadata>`**: Same but emitting `WrappedResponse<TValue, TMetadata>`.

Both accept an optional `statusCode` parameter (default 200) and a `contentType` parameter (default `"application/json"`).

### Test Project: `Light.Results.AspNetCore.Mvc.Tests`

Structure the tests analogously to the Minimal API test project:
- **Test app fixtures** using `WebApplication` + `TestServer` directly (matching the Minimal API test pattern, not `WebApplicationFactory<T>`) with MVC controllers that call `ToMvcActionResult`.
- **Integration tests** covering:
  - Success responses for `Result<T>` (value serialized as JSON, correct status code and content type).
  - Success responses for `Result` (empty body or metadata-only body, correct status code).
  - `ToHttp201CreatedMvcActionResult` with Location header.
  - Error responses producing Problem Detail JSON with correct status codes.
  - Metadata serialized to headers and/or body depending on `LightResultsHttpWriteOptions`.
  - `IHttpResultEnricher` integration.
  - Custom `JsonSerializerOptions` overrides.
  - Round-trip tests using `ReadResultAsync` from `Light.Results.Http.Reading`.
- Use Verify (snapshot testing) for response verification where appropriate, matching the Minimal API test patterns.

### Shared Type Relocation

Move `WrappedResponse<TValue, TMetadata>` from `Light.Results.AspNetCore.MinimalApis` to `Light.Results.AspNetCore.Shared` so both the Minimal API and MVC projects can reference it. Update the Minimal API project's `using` statements accordingly.
