# OpenAPI Support Redesign

## Rationale

Plan `0040-0-openapi-support.md` added OpenAPI support through schema-only CLR surrogate types (`PortableError<TMetadata>`, `PortableProblemDetails<TErrorMetadata, TProblemMetadata>`, and so on). The generic type parameters pollute the emitted OpenAPI schema names, which forced a workaround (`PortableResultsOpenApiNamingConventions.TryCreateSchemaReferenceId`) and a parallel `<object, object>` alias hierarchy purely for naming. The workaround only handles the `<object, object>` case; strongly typed metadata still produces names such as `PortableProblemDetailsOfMyErrorMetaAndMyProblemMeta`. The non-generic `PortableError` and `PortableValidationErrorDetail` classes are not reachable from any helper and duplicate the generic surface without adding value. Finally, the typed metadata generics promise a schema shape the runtime cannot honor: the runtime always serializes `MetadataObject` via `Utf8JsonWriter.WriteMetadataObject` (see `src/Light.PortableResults/SharedJsonSerialization/Writing/MetadataExtensions.cs`), not the caller's CLR type.

This redesign replaces the CLR-surrogate approach with a library-authored OpenAPI schema catalog and an `IOpenApiDocumentTransformer`. The library owns the envelope schemas directly (five canonical envelope components plus a shared `ErrorCategory` enum component) and injects them into the `OpenApiDocument`. Endpoint helpers and MVC attributes become thin markers that the transformer reads to emit operation responses. Per-error-code metadata contracts are registered once in DI and opted into per endpoint, with an inline escape hatch.

The entire OpenAPI-facing surface ships in a new dedicated package `Light.PortableResults.AspNetCore.OpenApi`. It depends on `Microsoft.AspNetCore.OpenApi` (which is not part of the `Microsoft.AspNetCore.App` shared framework and therefore must be referenced as a NuGet package) and project-references `Light.PortableResults.AspNetCore.Shared`. The runtime packages `Light.PortableResults.AspNetCore.MinimalApis` and `Light.PortableResults.AspNetCore.Mvc` do **not** take on a dependency on `Microsoft.AspNetCore.OpenApi` — consumers who want OpenAPI support opt in by also referencing the new package, so applications that never touch OpenAPI do not pay the transitive cost.

The redesign explicitly targets `Microsoft.AspNetCore.OpenApi` only. Swashbuckle / NSwag interop is a non-goal.

This plan supersedes the OpenAPI portions of `0040-0-openapi-support.md`. The breaking rename of `WrappedResponse<TValue, TMetadata>` to `PortableSuccessResponse<...>` that plan already landed is not reverted; the type is simply removed along with the rest of the schema-only surface. This is intentionally a breaking change to the OpenAPI-facing public surface of the ASP.NET Core packages; the root `AGENTS.md` explicitly permits breaking changes while the library is pre-stable.

## Acceptance Criteria

- [x] All schema-only CLR types introduced by `0040-0-openapi-support.md` are deleted: `PortableError`, `PortableError<TMetadata>`, `PortableValidationErrorDetail`, `PortableValidationErrorDetail<TMetadata>`, `PortableProblemDetails`, `PortableProblemDetails<TErrorMetadata, TProblemMetadata>`, `PortableRichValidationProblemDetails`, `PortableRichValidationProblemDetails<TErrorMetadata, TProblemMetadata>`, `PortableAspNetCoreValidationProblemDetails`, `PortableAspNetCoreValidationProblemDetails<TErrorDetailMetadata, TProblemMetadata>`, `PortableSuccessResponse<TValue, TMetadata>`.
- [x] `PortableResultsOpenApiNamingConventions` is deleted together with its tests.
- [x] All two-generic endpoint helpers on `PortableResultsEndpointExtensions` and all two-generic MVC attributes are deleted. The helper/attribute split between `Rich` and `AspNetCoreCompatible` validation problems is collapsed into a single helper/attribute.
- [x] The runtime packages `Light.PortableResults.AspNetCore.MinimalApis` and `Light.PortableResults.AspNetCore.Mvc` no longer expose any OpenAPI helper or attribute surface at all. Concretely, the entire `PortableResultsEndpointExtensions` class is deleted from `Light.PortableResults.AspNetCore.MinimalApis` (including every non-generic helper such as `ProducesPortableProblem`, `ProducesPortableRichValidationProblem`, and `ProducesPortableAspNetCoreValidationProblem`, not only the two-generic overloads), and `ProducesPortableSuccessResponseAttribute`, `ProducesPortableProblemAttribute`, `ProducesPortableRichValidationProblemAttribute`, and `ProducesPortableAspNetCoreValidationProblemAttribute` are deleted from `Light.PortableResults.AspNetCore.Mvc`. The replacements live exclusively in the new `Light.PortableResults.AspNetCore.OpenApi` package so there is a single public OpenAPI surface across the solution.
- [x] A new project `Light.PortableResults.AspNetCore.OpenApi` is added to the solution. It targets .NET 10, sets `<IsAotCompatible>true</IsAotCompatible>`, project-references `Light.PortableResults.AspNetCore.Shared`, carries a `<FrameworkReference Include="Microsoft.AspNetCore.App" />`, and takes on the NuGet `<PackageReference Include="Microsoft.AspNetCore.OpenApi" />` at the version already pinned in `Directory.Packages.props`. The runtime packages `Light.PortableResults.AspNetCore.MinimalApis` and `Light.PortableResults.AspNetCore.Mvc` do not gain this package reference.
- [x] `Light.PortableResults.AspNetCore.OpenApi` contains a library-authored OpenAPI schema catalog class named `PortableResultsOpenApiSchemas` that writes exactly five canonical envelope components into `OpenApiDocument.Components.Schemas` under the exact ids `PortableError`, `PortableValidationErrorDetail`, `PortableProblemDetails`, `PortableRichValidationProblemDetails`, and `PortableAspNetCoreValidationProblemDetails`, plus a supporting `ErrorCategory` enum component (six schema components total). The `metadata`, `errorDetails[*].metadata`, and `errors[*].metadata` slots are declared as open objects (`type: object, additionalProperties: true`) to match what `MetadataExtensions.WriteMetadataObject` actually emits. Success envelopes are not part of the canonical catalog; they are synthesized per operation by the transformer because they only take a stable shape in the context of a specific `TValue`.
- [x] `Light.PortableResults.AspNetCore.OpenApi` contains an `IOpenApiDocumentTransformer` implementation named `PortableResultsOpenApiDocumentTransformer` that (a) installs the canonical catalog once per document, (b) resolves the effective validation format from `PortableResultsHttpWriteOptions.ValidationProblemSerializationFormat` or the per-endpoint override, and (c) synthesizes any operation-specific derived schemas required by the markers attached to each endpoint.
- [x] `Light.PortableResults.AspNetCore.OpenApi` exposes a single opt-in entry point `AddPortableResultsOpenApi(this IServiceCollection services)` that registers `PortableResultsOpenApiDocumentTransformer` and its `ConfigureAll<OpenApiOptions>` hook idempotently (using `TryAddSingleton` for the transformer plus a private gate service so repeated calls register the configure-options callback exactly once). Consumers who want OpenAPI support call this alongside `AddPortableResultsForMinimalApis` and/or `AddPortableResultsForMvc`. `AddPortableResultsForMinimalApis` and `AddPortableResultsForMvc` do **not** transitively call `AddPortableResultsOpenApi`, so applications that never touch OpenAPI are unaffected. Callers do not need to configure `OpenApiOptions.CreateSchemaReferenceId`.
- [x] `Light.PortableResults.AspNetCore.OpenApi` exposes exactly the following `RouteHandlerBuilder` extension methods on `PortableResultsOpenApiRouteHandlerBuilderExtensions`, where `TValue` is the only generic on the public helper surface:
  - `ProducesPortableSuccessResponse<TValue>(this RouteHandlerBuilder builder, int statusCode = StatusCodes.Status200OK, string contentType = "application/json", Action<PortableSuccessResponseOpenApiBuilder>? configure = null)`
  - `ProducesPortableProblem(this RouteHandlerBuilder builder, int statusCode = StatusCodes.Status500InternalServerError, string contentType = "application/problem+json", Action<PortableProblemOpenApiBuilder>? configure = null)`
  - `ProducesPortableValidationProblem(this RouteHandlerBuilder builder, int statusCode = StatusCodes.Status400BadRequest, string contentType = "application/problem+json", Action<PortableValidationProblemOpenApiBuilder>? configure = null)`
- [x] `PortableSuccessResponseOpenApiBuilder` exposes `UseMetadataSerializationMode(MetadataSerializationMode mode)` as a per-endpoint static override, mirroring the existing `UseFormat(ValidationProblemSerializationFormat)` override on `PortableValidationProblemOpenApiBuilder`. The documented schema is selected by the transformer from the resolved mode, so callers can either take the DI default or override it per endpoint for documentation purposes. It remains the caller's responsibility to align any runtime `overrideOptions` passed to `LightResult<T>` / `LightActionResult<T>` with the documented mode.
- [x] `Light.PortableResults.AspNetCore.OpenApi` exposes exactly three attributes: `ProducesPortableSuccessResponseAttribute<TValue>`, `ProducesPortableProblemAttribute`, and `ProducesPortableValidationProblemAttribute`. Each is `sealed` and works for both MVC controllers (applied to an action method) and Minimal APIs (the corresponding helper constructs and attaches an instance via `RouteHandlerBuilder.WithMetadata`). They sit in a three-level hierarchy designed so that every public knob is applicable to the type it is declared on (no silent ignores): a public abstract base `PortableOpenApiResponseAttributeBase : Attribute` carries only the truly shared knobs (`Kind`, `StatusCode`, `ContentType`, `TopLevelMetadataType`); a public abstract intermediate `PortableOpenApiErrorResponseAttributeBase : PortableOpenApiResponseAttributeBase` adds the error-list knobs (`ErrorCodes`, `InlineErrorMetadataCodes`, `InlineErrorMetadataTypes`); the three sealed attributes add kind-specific knobs directly — the success attribute adds `ValueType` (set in its constructor from `typeof(TValue)`) and `MetadataSerializationMode`, the problem attribute adds nothing beyond the error base, the validation attribute adds `Format`. Each sealed attribute exposes a constructor accepting `(int statusCode, string contentType)` with defaults matching its Minimal APIs counterpart (`200` / `application/json` for the success attribute, `500` / `application/problem+json` for the problem attribute, `400` / `application/problem+json` for the validation attribute), so call sites read naturally as `[ProducesPortableProblem(404)]` rather than `[ProducesPortableProblem(StatusCode = 404)]`. The base is intentionally not derived from `ProducesResponseTypeAttribute` because the transformer owns schema selection end-to-end; a consequence is that MVC filters and analyzers that enumerate `ProducesResponseTypeAttribute` (for example the default `ApiExplorer` content-negotiation behavior) will not see Light.PortableResults responses, and the document transformer is the single source of truth for these operations. MVC attribute instances enter endpoint metadata through the standard MVC endpoint-routing pipeline (attributes on a controller action are added to `ActionDescriptor.EndpointMetadata` automatically), so the attributes do not need to implement `IEndpointMetadataProvider`.
- [x] A global error-code metadata registry is exposed through the extension method `ConfigureErrorMetadataContracts(this IServiceCollection services, Action<PortableErrorMetadataContractsBuilder> configure)` declared in `Light.PortableResults.AspNetCore.OpenApi`. `PortableErrorMetadataContractsBuilder` exposes `ForCode<TMetadata>(string code)` and `ForCode(string code, Type metadataType)` registration methods. The registrations are stored in a singleton service `IPortableErrorMetadataContractRegistry` with an immutable `IReadOnlyDictionary<string, Type> Contracts` property. Registered codes are synthesized into `PortableError__<SanitizedCode>` and `PortableValidationErrorDetail__<SanitizedCode>` schema components once per document (see the sanitization criterion below); endpoints opt into specific codes via `WithErrorCodes(params string[])`. When `WithErrorCodes` references a code that is not present in `IPortableErrorMetadataContractRegistry.Contracts`, the transformer throws `InvalidOperationException` at document generation with a message that names the unregistered code and suggests either registering it through `ConfigureErrorMetadataContracts` or using the inline `WithErrorMetadata` escape hatch. Inline escape hatches `WithErrorMetadata(string code, Type metadataType)` and `WithErrorMetadata<TMetadata>(string code)` are available on the problem and validation-problem endpoint builders for codes that are not globally registered.
- [x] `ConfigureErrorMetadataContracts` is implemented on top of the standard .NET options pipeline: it wraps the caller's `Action<PortableErrorMetadataContractsBuilder>` in a `services.Configure<PortableErrorMetadataContractsOptions>(...)` registration (where `PortableErrorMetadataContractsOptions` is a small public options type owning a single `Builder` property), and registers `IPortableErrorMetadataContractRegistry` via `TryAddSingleton` with a factory that materializes the immutable registry from `IOptions<PortableErrorMetadataContractsOptions>.Value.Builder`. This gives additive composition for free: multiple invocations (for example from separate feature modules during composition-root setup) each register another `IConfigureOptions<PortableErrorMetadataContractsOptions>` that runs in registration order against the same lazily-created options instance. Registering the same raw code twice with the same `Type` is an idempotent no-op. Registering the same raw code twice with two different `Type`s throws `InvalidOperationException` with a message naming the raw code and both conflicting types; the throw fires either inside `PortableErrorMetadataContractsBuilder.ForCode` (when the conflict is observable to the builder at configure time) or at registry materialization (when two independent configure callbacks contribute conflicting entries).
- [x] Per-endpoint metadata narrowing is expressed in the emitted OpenAPI document using `allOf` to extend a canonical envelope and `anyOf + discriminator` on the error `code` property to narrow `errors[*]` (rich format) or `errorDetails[*]` (asp.net-core-compatible format). The transformer emits an explicit `discriminator.mapping` whose keys are the raw code strings as they appear on the wire and whose values are JSON-Pointer-escaped `$ref`s to the synthesized variants (for example `VersionMismatch: '#/components/schemas/PortableError__VersionMismatch'`), because implicit discriminator resolution matches on bare component name and our synthesized component ids are `PortableError__<SanitizedCode>`. A fallback `$ref` to the base `PortableError` / `PortableValidationErrorDetail` schema is always included as the last branch of the `anyOf` so that undocumented codes remain valid; `anyOf` is used instead of `oneOf` because every narrowed variant is an `allOf` restriction of the base schema and would therefore also match the base, which violates `oneOf` semantics.
- [x] The transformer applies a deterministic sanitization scheme to every error code used in a component id: characters outside `[A-Za-z0-9_]` are replaced with `_`. Collisions after sanitization are rejected at the earliest possible moment: `ConfigureErrorMetadataContracts` throws `InvalidOperationException` at registration time when two distinct globally registered codes sanitize to the same id, and the transformer throws `InvalidOperationException` at document generation time when two distinct inline `WithErrorMetadata` codes on the same `(operation, StatusCode, ContentType)` triple sanitize to the same suffix; both messages name the conflicting raw codes. The discriminator `mapping` keys use the unsanitized raw code (matching what the runtime writes to the `code` property), and the discriminator `mapping` values and operation-level `$ref`s apply JSON Pointer escaping per RFC 6901 (`~` → `~0`, `/` → `~1`) defensively. Sanitization applies identically to `PortableError__<SanitizedCode>`, `PortableValidationErrorDetail__<SanitizedCode>`, and operation-scoped inline variants.
- [x] The runtime HTTP serialization behavior of `LightResult`, `LightResult<T>`, `LightActionResult`, `LightActionResult<T>`, and the JSON writers in `Light.PortableResults` is unchanged.
- [x] The transformer resolves the target OpenAPI spec version from a single source of truth: `OpenApiOptions.OpenApiVersion` for the current document, obtained via `context.ApplicationServices.GetRequiredService<IOptionsMonitor<OpenApiOptions>>().Get(context.DocumentName)`. It emits discriminator narrowing using schema-level `const` when the resolved version is OpenAPI 3.1 or later, and falls back to `enum: [<Code>]` for OpenAPI 3.0. Generated schemas are spec-valid against both versions.
- [x] When multiple `PortableOpenApiResponseAttributeBase` instances share the same `(StatusCode, ContentType)` key on the same operation, the transformer treats them as distinct contributing schemas for the same HTTP response and merges them into a single `OpenApiResponse` whose media-type schema is an `anyOf` over the contributing envelopes, so common designs such as documenting both a `ProducesPortableProblemAttribute(400)` and a `ProducesPortableValidationProblemAttribute(400)` on the same endpoint at `application/problem+json` produce one response entry with a unioned schema. The transformer still throws `InvalidOperationException` at document generation time when more than one marker of the same `Kind` is attached to the same operation for the same `(StatusCode, ContentType)` key (for example two `ProducesPortableProblemAttribute`s with identical status and content type), because that is a genuine ambiguity about which narrowing to emit. It also throws `InvalidOperationException` when an attribute instance has both `InlineErrorMetadataCodes` and `InlineErrorMetadataTypes` set to non-null arrays of different lengths; the exception message includes both observed lengths so the caller can realign them.
- [x] The `PackageReleaseNotes` section of `Light.PortableResults.AspNetCore.MinimalApis.csproj` and `Light.PortableResults.AspNetCore.Mvc.csproj` is updated to call out the removal of the schema-only CLR types and the helper/attribute collapse, and to point consumers at the new `Light.PortableResults.AspNetCore.OpenApi` package for OpenAPI integration. `Light.PortableResults.AspNetCore.OpenApi.csproj` carries its own `PackageReleaseNotes` introducing the package, its opt-in `AddPortableResultsOpenApi` entry point, the canonical schema catalog, the three helpers, the three attributes, and the error-metadata registry.
- [x] Automated tests cover the document transformer end-to-end: canonical catalog emission, each helper/attribute's effect on the generated document, global error-code registry integration, inline escape hatch, per-endpoint format override, success-response metadata narrowing, and the fallback `$ref` for undocumented codes.
- [x] The `NativeAotMovieRating` sample is updated to the new public API (adds a `ProjectReference` to `Light.PortableResults.AspNetCore.OpenApi`, calls `AddPortableResultsOpenApi()`, uses the new helpers) and no longer wires `OpenApiOptions.CreateSchemaReferenceId`.
- [x] The new OpenAPI surface remains NativeAOT-compatible. The `NativeAotMovieRating` sample continues to build and run under `PublishAot=true`, and the document transformer uses only APIs compatible with the trimmer and AOT analyzer (no `Type.MakeGenericType`, no dynamic assembly emit, no reflection over handler parameters; all generic dispatch happens through the existing `GetOrCreateSchemaAsync` API and through attribute instances supplied at compile time).
- [x] `README.md` is updated: the OpenAPI section reflects the new public surface (new `Light.PortableResults.AspNetCore.OpenApi` package, opt-in `AddPortableResultsOpenApi()`, three helpers, three attributes, DI-level `ConfigureErrorMetadataContracts`, per-endpoint format override), and all references to the deleted schema-only CLR types, the naming convention, and the `Rich` vs `AspNetCoreCompatible` helper split are removed.

## Technical Details

### Ownership Model

- **Envelope schemas are library-owned.** They are authored once, in OpenAPI notation, in `Light.PortableResults.AspNetCore.OpenApi`. They do not exist as CLR types.
- **Metadata content is caller-owned.** By default metadata slots are declared as open objects. The endpoint builder is the single place where narrowing is expressed per endpoint: it narrows the top-level `metadata` schema via `WithMetadata<T>`, opts into globally registered per-code contracts via `WithErrorCodes`, and overrides one-off codes inline via `WithErrorMetadata`. The global `ConfigureErrorMetadataContracts` registry is a complementary mechanism that stores the per-code metadata contracts the builder references, not an alternative to it. Typical apps use both: register each stable error code once in DI, then opt the relevant codes into each failure response on the endpoint.
- **Validation format is per-endpoint with a DI default.** The runtime already supports this through `PortableResultsHttpWriteOptions.ValidationProblemSerializationFormat` plus the per-call override path in `HttpExtensions.ResolvePortableResultsHttpWriteOptions`. The OpenAPI helper mirrors this: if no `format` is passed, the configured app default is used.
- **Documentation overrides are declarative and static, runtime overrides are per-call.** Both `UseMetadataSerializationMode(...)` and `UseFormat(...)` attach endpoint metadata that the transformer reads at document generation time. Runtime handlers use a separate override path (`LightResult<T>` / `LightActionResult<T>` constructors accept `PortableResultsHttpWriteOptions? overrideOptions`). The library cannot observe runtime overrides from a static transformer, so callers who override runtime options per endpoint must pass a matching declarative override to the OpenAPI helper/attribute to keep the documented shape aligned with the wire. A future plan may unify these two paths by having `HttpContext.ResolvePortableResultsHttpWriteOptions` consult endpoint metadata as an additional fallback step; that runtime change is explicitly out of scope here.
- **Success-response shape is mode-aware.** The runtime produces two distinct body shapes for `LightResult<T>` (see the `IsValid` branch in `HttpResultForWritingJsonConverter<T>`): a bare `T` under `MetadataSerializationMode.ErrorsOnly`, and a wrapped `{ value: T, metadata?: object }` under `MetadataSerializationMode.Always` via `SerializeValueAndMetadata`, where `metadata` is only written when any metadata value is annotated `SerializeInHttpResponseBody`. `ProducesPortableSuccessResponse<TValue>` is faithful to both modes: the transformer resolves the effective mode from `attr.MetadataSerializationMode ?? options.Value.MetadataSerializationMode`, calls `context.GetOrCreateSchemaAsync(attr.ValueType!)` to obtain the value-type schema (which may be inline for primitives and collections, or a reference for complex types), and either installs that schema directly on the response (under `ErrorsOnly` with no narrowing) or wraps it in a per-operation envelope component registered via `document.AddComponent` (under `Always` or whenever `TopLevelMetadataType` is set). Non-generic `LightResult` / `LightActionResult` success responses are out of scope for this helper; callers document them with plain ASP.NET Core helpers (`Produces()`, `ProducesResponseType()`, or status-only responses).

### Canonical Schema Catalog

A single static class `PortableResultsOpenApiSchemas` in `Light.PortableResults.AspNetCore.OpenApi` produces the canonical schemas and installs them into `OpenApiDocument.Components.Schemas`. Its only public method is `InstallInto(OpenApiDocument document)`, which is idempotent (keyed by schema component id) and initializes `document.Components` and `document.Components.Schemas` if either is null. Tests assert on the installed components by calling `InstallInto` on a fresh `OpenApiDocument` and inspecting `document.Components.Schemas`. Each schema is authored directly as `OpenApiSchema` objects from `Microsoft.OpenApi.Models`.

Schema shapes mirror what the runtime actually writes:

- `PortableError`: `message` (string, required), `code` (string, nullable), `target` (string, nullable), `category` (`$ref: ErrorCategory`), `metadata` (open object, nullable).
- `PortableValidationErrorDetail`: `target` (string, required), `index` (integer, required), `code` (string, nullable), `category` (`$ref: ErrorCategory`, nullable), `metadata` (open object, nullable).
- `PortableProblemDetails`: extends RFC 9457 Problem Details with `errors` (array of `PortableError`) and `metadata` (open object, nullable).
- `PortableRichValidationProblemDetails`: same shape as `PortableProblemDetails` but a distinct schema component so generated client code can distinguish validation failures.
- `PortableAspNetCoreValidationProblemDetails`: extends `HttpValidationProblemDetails` with optional `errorDetails` (array of `PortableValidationErrorDetail`) and `metadata` (open object, nullable).

Success responses are intentionally absent from the catalog. They only take a stable shape in the context of a specific `TValue`, and the transformer synthesizes each one per operation via `document.AddComponent` (where `document` is the `OpenApiDocument` parameter passed to `TransformAsync`). The shape of every synthesized success envelope is `{ value: <TValueSchema>, metadata: open object (nullable) }`.

The `ErrorCategory` enum is also emitted as a schema component once under the id `ErrorCategory`, reused by all envelopes.

### Endpoint Metadata Attributes

There is no separate marker POCO. The three sealed attribute types are themselves the endpoint-metadata entries the transformer reads from `apiDescription.ActionDescriptor.EndpointMetadata`. They sit in a three-level hierarchy chosen so that every public knob is valid on the type it is declared on — there are no silent-ignore properties on any public attribute.

```csharp
public abstract class PortableOpenApiResponseAttributeBase : Attribute
{
    protected PortableOpenApiResponseAttributeBase(
        PortableOpenApiResponseKind kind,
        int statusCode,
        string contentType)
    {
        Kind = kind;
        StatusCode = statusCode;
        ContentType = contentType;
    }

    public PortableOpenApiResponseKind Kind { get; }
    public int StatusCode { get; set; }
    public string ContentType { get; set; }
    public Type? TopLevelMetadataType { get; set; }
}

public abstract class PortableOpenApiErrorResponseAttributeBase : PortableOpenApiResponseAttributeBase
{
    protected PortableOpenApiErrorResponseAttributeBase(
        PortableOpenApiResponseKind kind,
        int statusCode,
        string contentType)
        : base(kind, statusCode, contentType) { }

    public string[]? ErrorCodes { get; set; }
    public string[]? InlineErrorMetadataCodes { get; set; }
    public Type[]? InlineErrorMetadataTypes { get; set; }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class ProducesPortableSuccessResponseAttribute<TValue> : PortableOpenApiResponseAttributeBase
{
    public ProducesPortableSuccessResponseAttribute(
        int statusCode = StatusCodes.Status200OK,
        string contentType = "application/json")
        : base(PortableOpenApiResponseKind.SuccessResponse, statusCode, contentType)
    {
        ValueType = typeof(TValue);
    }

    public Type ValueType { get; }
    public MetadataSerializationMode? MetadataSerializationMode { get; set; }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class ProducesPortableProblemAttribute : PortableOpenApiErrorResponseAttributeBase
{
    public ProducesPortableProblemAttribute(
        int statusCode = StatusCodes.Status500InternalServerError,
        string contentType = "application/problem+json")
        : base(PortableOpenApiResponseKind.Problem, statusCode, contentType) { }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class ProducesPortableValidationProblemAttribute : PortableOpenApiErrorResponseAttributeBase
{
    public ProducesPortableValidationProblemAttribute(
        int statusCode = StatusCodes.Status400BadRequest,
        string contentType = "application/problem+json")
        : base(PortableOpenApiResponseKind.ValidationProblem, statusCode, contentType) { }

    public ValidationProblemSerializationFormat? Format { get; set; }
}
```

`PortableOpenApiResponseKind` is an enum with values `SuccessResponse`, `Problem`, `ValidationProblem`. `AllowMultiple = true` lets a single operation declare several response contracts per kind (for example two distinct `[ProducesPortableProblem]` status codes, or a problem plus a validation problem at the same status code — see the merge rule in the *Document Transformer* section).

Minimal APIs helpers construct a concrete attribute instance, pass it into the corresponding configuration builder (which only exposes members that map onto settable properties actually present on that concrete attribute), and then call `RouteHandlerBuilder.WithMetadata(attributeInstance)`. MVC attribute instances flow into `ActionDescriptor.EndpointMetadata` through the standard MVC endpoint-routing pipeline. Both paths converge on the same metadata hierarchy, and the transformer reads them uniformly via `apiDescription.ActionDescriptor.EndpointMetadata.OfType<PortableOpenApiResponseAttributeBase>()`, then branches on concrete type for kind-specific logic. No reflection over handler parameters is needed, and the design is AOT-friendly.

### Document Transformer

`PortableResultsOpenApiDocumentTransformer` (in `Light.PortableResults.AspNetCore.OpenApi`) is a singleton service implementing `IOpenApiDocumentTransformer`. Its constructor takes `IOptions<PortableResultsHttpWriteOptions>` and `IPortableErrorMetadataContractRegistry`. The transformer holds no mutable instance state between invocations: all per-document state lives on the passed `OpenApiDocument` and transformer context, so it is safe to register as a singleton and to run concurrently across multiple OpenAPI documents. Its `TransformAsync` signature is `TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)`: every component registration writes to the `document` parameter directly (for example `document.AddComponent(name, schema)`), because `OpenApiDocumentTransformerContext` does not expose a `Document` property — it provides only `DocumentName`, `DescriptionGroups`, `ApplicationServices`, and the `GetOrCreateSchemaAsync` method. The target OpenAPI spec version is resolved per invocation from `context.ApplicationServices.GetRequiredService<IOptionsMonitor<Microsoft.AspNetCore.OpenApi.OpenApiOptions>>().Get(context.DocumentName).OpenApiVersion`, so a single source of truth (`OpenApiOptions.OpenApiVersion`) drives all spec-version-dependent branches. `TransformAsync`:

1. On first invocation for a given document, calls `PortableResultsOpenApiSchemas.InstallInto(document)` (idempotent: checked by schema component id).
2. For each registered entry in `IPortableErrorMetadataContractRegistry.Contracts`, synthesizes the global `PortableError__<SanitizedCode>` and `PortableValidationErrorDetail__<SanitizedCode>` schema components once, applying the error-code sanitization rule described in the Acceptance Criteria.
3. Iterates the `ApiDescription` instances exposed through `context.DescriptionGroups`, reads `PortableOpenApiResponseAttributeBase` entries via `apiDescription.ActionDescriptor.EndpointMetadata.OfType<PortableOpenApiResponseAttributeBase>()` (working uniformly for Minimal APIs and MVC), and locates the matching `OpenApiOperation` by translating the `ApiDescription` into `OpenApiDocument.Paths` keys: the path key is `"/" + apiDescription.RelativePath` when `RelativePath` does not already start with `/`, and the operation key is obtained by parsing `apiDescription.HttpMethod` into `Microsoft.OpenApi.Models.OperationType` (case-insensitive `Enum.Parse`). The resolved `OpenApiOperation` has its `Responses` collection mutated in place. The transformer groups the attributes by `(StatusCode, ContentType)`: if any group contains more than one attribute of the same `Kind`, it throws `InvalidOperationException` with a message naming the status, content type, and kind. Groups that contain multiple attributes of different `Kind`s are accepted and merged in step 4 so that documenting both a problem and a validation problem at the same `(StatusCode, ContentType)` — a common cloud-API shape — produces one `OpenApiResponse` with a unioned schema.
4. For each `(StatusCode, ContentType)` group on each operation, builds a list of contributing schemas — one per attribute in the group — and then attaches them to the `OpenApiResponse` for that status/media type:
   - If the group has one contributing schema, it is used as the response content schema directly.
   - If the group has more than one contributing schema (necessarily of different `Kind`s per the rule in step 3), the response content schema is `anyOf` over the contributing schemas in the order the attributes were discovered. `anyOf` is used instead of `oneOf` for the same reason as in the error-narrowing design: the contributing envelopes can overlap structurally (both problem variants extend RFC 9457 Problem Details), so `oneOf`'s exclusivity rule would be violated.

   Each contributing schema is built by dispatching on the attribute's concrete type:

   - **`ProducesPortableSuccessResponseAttribute<TValue>`** resolves the effective mode as `attr.MetadataSerializationMode ?? options.Value.MetadataSerializationMode` and obtains the value-type schema via `await context.GetOrCreateSchemaAsync(attr.ValueType, parameterDescription: null, cancellationToken)` (the second argument is always null for response types because `ApiParameterDescription` carries request-parameter context only). The returned `OpenApiSchema` may be inline (primitives, collections) or a reference to an existing component (complex types) — the transformer uses it as-is wherever a value schema is required.
     - Under `ErrorsOnly` with no `TopLevelMetadataType`, the contributing schema is the returned `OpenApiSchema` directly. No envelope component is registered.
     - Under `Always` (or whenever `TopLevelMetadataType` is set), the transformer synthesizes an operation-scoped envelope component whose `value` property's schema is the returned `OpenApiSchema` and whose `metadata` property is either the open-object canonical or — when `TopLevelMetadataType` is set — the schema produced by `GetOrCreateSchemaAsync(attr.TopLevelMetadataType)`. The envelope is registered via `document.AddComponent(name, envelopeSchema)` and referenced from the contributing schema. Per-operation synthesis (rather than reuse by `TValue`) is mandatory because ASP.NET Core leaves primitive and collection schemas inline — there is no stable `TValueSchemaId` to key reuse on for those payloads.
     - If `TopLevelMetadataType` is set and the resolved mode is `ErrorsOnly`, the transformer throws `InvalidOperationException` at document generation because metadata is not part of the wire in that mode.
   - **`ProducesPortableProblemAttribute`** and **`ProducesPortableValidationProblemAttribute`** (via the shared `PortableOpenApiErrorResponseAttributeBase`) reference the canonical schema directly by `$ref` when unconfigured, or produce a derived `allOf` envelope registered via `document.AddComponent` when any of `TopLevelMetadataType`, `ErrorCodes`, or `InlineErrorMetadataCodes` is set. Before synthesizing the operation-scoped inline variants, the transformer sanitizes each inline code and throws `InvalidOperationException` if two distinct inline codes on this `(operation, StatusCode, ContentType)` triple collide after sanitization; the exception names both raw codes so the caller can rename or globally register one of them.
   - **Synthesized schema names** follow `<CanonicalName>__<OperationId>__<StatusCode>__<SanitizedContentType>` (for example `PortableProblemDetails__GetMovies__404__application_problem_json`) and fall back to `<CanonicalName>__<HttpMethod>__<SanitizedRoutePattern>__<StatusCode>__<SanitizedContentType>` when the operation has no `OperationId`. Segments are always separated by `__` (double underscore) so that component ids are visually and programmatically parseable into their constituent parts; characters within each segment are restricted to `[A-Za-z0-9_]` by sanitization so `__` is unambiguous as a segment delimiter. `SanitizedContentType` replaces `/`, `+`, `.`, `-`, and any other non-`[A-Za-z0-9_]` character with `_`. `SanitizedRoutePattern` applies the same rule to the raw route template and collapses adjacent replacement characters to a single `_`, so `/api/movies/{id}` becomes `api_movies_id`. Including the content-type token is required because one operation may legitimately document different narrowings for the same status code under different content types, and the transformer's duplicate-attribute check is keyed by `(StatusCode, ContentType)`.
5. Resolves the effective validation format for `Kind == ValidationProblem` attributes as `attr.Format ?? options.Value.ValidationProblemSerializationFormat`. The chosen format selects `PortableRichValidationProblemDetails` or `PortableAspNetCoreValidationProblemDetails` as the base schema in the `allOf`.
6. Emits discriminator narrowing using schema-level `const` when the resolved `OpenApiVersion` is OpenAPI 3.1 or later, and falls back to `enum: [<Code>]` for OpenAPI 3.0.
7. Emits metadata DTO schemas (for `TopLevelMetadataType`, registry entries, and `InlineErrorMetadata` values) by calling `context.GetOrCreateSchemaAsync` on the CLR type and, when the transformer needs a stable reference, explicitly registering the returned schema via `document.AddComponent`. This keeps serializer configuration and polymorphism intact while ensuring every `$ref` the transformer emits points at a component it has actually registered.

### Per-Error-Code Metadata Registry

`IPortableErrorMetadataContractRegistry` (declared in `Light.PortableResults.AspNetCore.OpenApi`) is a singleton service with one property, `IReadOnlyDictionary<string, Type> Contracts`. Its default implementation, `PortableErrorMetadataContractRegistry`, is materialized from a `PortableErrorMetadataContractsBuilder` that callers populate through the DI extension method `ConfigureErrorMetadataContracts(this IServiceCollection services, Action<PortableErrorMetadataContractsBuilder> configure)`. The builder exposes `ForCode<TMetadata>(string code)` and `ForCode(string code, Type metadataType)` and is internally a `Dictionary<string, Type>`.

Additive composition is delegated to the standard .NET options pipeline rather than hand-rolled. A small public options type `PortableErrorMetadataContractsOptions` owns a single `PortableErrorMetadataContractsBuilder Builder { get; } = new();` property. `ConfigureErrorMetadataContracts` is implemented as:

```csharp
public static IServiceCollection ConfigureErrorMetadataContracts(
    this IServiceCollection services,
    Action<PortableErrorMetadataContractsBuilder> configure)
{
    services.AddOptions<PortableErrorMetadataContractsOptions>();
    services.Configure<PortableErrorMetadataContractsOptions>(opts => configure(opts.Builder));
    services.TryAddSingleton<IPortableErrorMetadataContractRegistry>(sp =>
        new PortableErrorMetadataContractRegistry(
            sp.GetRequiredService<IOptions<PortableErrorMetadataContractsOptions>>().Value.Builder));
    return services;
}
```

Each call to `ConfigureErrorMetadataContracts` registers another `IConfigureOptions<PortableErrorMetadataContractsOptions>` that runs in registration order against the same lazily-created options instance, so multiple calls from separate feature modules compose additively without any shared mutable service. `PortableErrorMetadataContractRegistry`'s constructor copies the builder's dictionary into an immutable snapshot, so the registry is frozen for the lifetime of the singleton.

`PortableErrorMetadataContractsBuilder.ForCode` enforces the duplicate rule directly where the conflict is observable: registering a raw code whose existing entry already has the same `Type` is a no-op; registering it with a different `Type` throws `InvalidOperationException` naming the raw code and both types. The `PortableErrorMetadataContractRegistry` constructor repeats the same check while snapshotting the builder, so a conflict introduced by a late-running `IConfigureOptions` callback is still caught deterministically at materialization time with the same exception message.

On first document generation the transformer synthesizes one `PortableError__<SanitizedCode>` schema per registered code using the `allOf` pattern (the `code` constraint is encoded as `const` on OpenAPI 3.1+ and as `enum: [<RawCode>]` on OpenAPI 3.0, and the component id applies the sanitization rule — any character outside `[A-Za-z0-9_]` replaced with `_`, collisions rejected at registration time):

```text
allOf:
  - $ref: PortableError
  - properties:
      code:     { type: string, const: <RawCode> }
      metadata: { $ref: <MetadataDto> }
    required: [code]
```

And one `PortableValidationErrorDetail__<SanitizedCode>` companion using the same pattern against `PortableValidationErrorDetail`.

Endpoints that call `WithErrorCodes(...)` cause the transformer to synthesize a derived envelope whose `errors[*]` (rich + generic problem) or `errorDetails[*]` (asp.net-core-compatible validation) array item is an `anyOf` over the narrowed code schemas plus a trailing `$ref` to the baseline for undocumented codes, with a `discriminator` on `code` carrying an explicit `mapping` entry for every documented code:

```text
anyOf:
  - $ref: '#/components/schemas/PortableError__VersionMismatch'
  - $ref: '#/components/schemas/PortableError__InsufficientFunds'
  - $ref: '#/components/schemas/PortableError'   # fallback for undocumented codes
discriminator:
  propertyName: code
  mapping:
    VersionMismatch:   '#/components/schemas/PortableError__VersionMismatch'
    InsufficientFunds: '#/components/schemas/PortableError__InsufficientFunds'
```

`anyOf` is used instead of `oneOf` because every narrowed variant is an `allOf` restriction of the base `PortableError`, so any narrowed instance also validates against the base; that would violate `oneOf`'s exclusivity rule. Under `anyOf`, validators accept the instance against at least one branch, and discriminator-aware tooling uses the explicit `mapping` to pick the precise narrowed variant by code. Explicit `mapping` is required because implicit discriminator resolution matches on bare component names and our synthesized component ids are `PortableError__<SanitizedCode>` rather than `<Code>`.

The discriminator `mapping` keys are the raw wire codes (matching what the runtime writes to the `code` property), and the mapping values are JSON-Pointer-escaped `$ref`s per RFC 6901. Because the component id is always pre-sanitized to `[A-Za-z0-9_]` the `$ref` value rarely needs escaping in practice, but the transformer applies the escape unconditionally so that any change to the sanitization rule remains spec-valid.

Inline `WithErrorMetadata(code, type)` follows the same mechanism but emits the synthesized narrowing schema scoped to the operation (for example `PortableError__GetMovies__409__application_problem_json__VersionMismatch`) so it does not pollute the global `PortableError__<SanitizedCode>` namespace. The per-operation name includes the sanitized content type to prevent collisions when the same operation documents different narrowings per media type, and is also registered in the discriminator mapping for the operation's envelope.

### Public API Shape

The Minimal APIs helpers return a builder from the configuration callback. Three sealed builder classes cover the three response kinds. They do not share a public base: each one exposes exactly the members that are applicable to the corresponding concrete attribute, so there are no silent-ignore builder methods.

- `PortableSuccessResponseOpenApiBuilder` — `WithMetadata<T>()`, `WithMetadata(Type metadataType)`, `UseMetadataSerializationMode(MetadataSerializationMode mode)`.
- `PortableProblemOpenApiBuilder` — `WithMetadata<T>()`, `WithMetadata(Type metadataType)`, `WithErrorCodes(params string[] codes)`, `WithErrorMetadata(string code, Type metadataType)`, `WithErrorMetadata<TMetadata>(string code)`.
- `PortableValidationProblemOpenApiBuilder` — the same surface as `PortableProblemOpenApiBuilder` plus `UseFormat(ValidationProblemSerializationFormat format)`.

All three builders are `sealed`. Each builder returns `this` from every method for chaining. Each method mutates settable properties on the paired concrete attribute instance the helper created up front (`PortableSuccessResponseOpenApiBuilder` mutates a `ProducesPortableSuccessResponseAttribute<TValue>`, and so on); after the configure callback returns, the helper calls `RouteHandlerBuilder.WithMetadata(attributeInstance)` to attach the attribute as endpoint metadata.

MVC attributes are the same sealed types described in the *Endpoint Metadata Attributes* section. Their settable properties use attribute-argument-compatible types per ECMA-335 §II.23.3 (primitives, `string`, `System.Type`, enums, or single-dimension arrays of those) — for example `string[]` instead of `IReadOnlyList<string>`. Each attribute only exposes properties that are meaningful for its kind: the success attribute has no `ErrorCodes`, the problem and validation attributes have no `MetadataSerializationMode`, and only the validation attribute has `Format`. This is enforced by the type hierarchy rather than by runtime validation.

Attribute instances reach `ActionDescriptor.EndpointMetadata` through the standard MVC endpoint-routing pipeline (attributes declared on a controller action are added automatically), so no `IEndpointMetadataProvider` implementation is needed. The transformer reads the same attribute instances that the Minimal APIs helpers attach via `RouteHandlerBuilder.WithMetadata(...)`, which gives both stacks a single metadata hierarchy rooted at `PortableOpenApiResponseAttributeBase`.

The Minimal APIs helper and MVC attribute for success responses keep `TValue` as the only generic parameter and do not expose error-code members (success responses do not carry errors).

### Scope Boundaries

- This feature does not change runtime JSON serialization.
- This feature does not infer schemas from `PortableResultsHttpWriteOptions` or handler signatures beyond what the markers explicitly declare.
- This feature does not support Swashbuckle / NSwag. Consumers of those stacks continue to receive the runtime wire format but no Light.PortableResults-specific OpenAPI helpers.
- This feature does not attempt to represent `MetadataValueAnnotation` filtering in OpenAPI. The schema documents the broadest possible metadata shape; runtime filtering by annotation remains a runtime concern.
- This feature does not ship built-in error-code contracts for the validation package. The built-in `ValidationErrorDefinition` classes (in `Light.PortableResults.Validation`) already define a stable code-plus-metadata taxonomy via `ValidationErrorMetadataKeys`, and a follow-up plan (`0040-2-validation-error-contracts.md`) will wire them into `IPortableErrorMetadataContractRegistry` through an opt-in `RegisterBuiltInValidationErrors()` extension. This redesign keeps the registry surface minimal (type-based contracts only) and is forward-compatible: the follow-up widens the contract value from `Type` to a discriminated union that also accepts pre-authored `OpenApiSchema` instances without breaking existing registrations.
