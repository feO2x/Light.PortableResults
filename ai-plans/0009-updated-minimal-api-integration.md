# Updated Design Summary vs Original Plan (0009)

This document summarizes how the current branch’s implementation differs from the original plan in
`ai-plans/0009-minimal-api-metadata-for-success-results.md`.

**Configuration And Scope**
- Plan: opt-in per call via a `metadataMode` parameter on `ToMinimalApiResult`/`LightResult`. Actual: `MetadataSerializationMode` is configured via `LightResultOptions` resolved from DI, with per-call overrides via `overrideOptions` only. (`src/Light.Results.AspNetCore.MinimalApis/MinimalApiResultExtensions.cs`, `src/Light.Results.AspNetCore.Shared/LightResultOptions.cs`)
- Plan: Minimal API only. Actual: serialization options and converters live in `Light.Results.AspNetCore.Shared` and are registered into `JsonOptions` by `AddLightResultsForMinimalApis`, so `Result` serialization behavior is shared across ASP.NET Core contexts. (`src/Light.Results.AspNetCore.MinimalApis/Module.cs`)

**Result Types And Serialization Pipeline**
- Plan: dedicated `LightSuccessResultJsonConverter<T>` and `LightResultJsonConverter` tied to `LightResult`/`LightResult<T>`. Actual: `DefaultResultJsonConverter` and `DefaultResultJsonConverterFactory` serialize `Result`/`Result<T>` directly, and `BaseLightResult` delegates to JsonSerializer/JsonTypeInfo. (`src/Light.Results.AspNetCore.Shared/Serialization/DefaultResultJsonConverter.cs`, `src/Light.Results.AspNetCore.MinimalApis/BaseLightResult.cs`)
- Plan: explicit wrapper semantics for non-generic results when `Always` is used. Actual: `Result` with `MetadataSerializationMode.Always` writes `{ "metadata": ... }` only when metadata contains at least one value annotated for response-body serialization; otherwise the body is empty (no wrapper). (`src/Light.Results.AspNetCore.Shared/Serialization/DefaultResultJsonConverter.cs`)
- Plan: for generic success wrappers, metadata is part of the wrapped schema. Actual: metadata is omitted from `{ "value": ... }` when no metadata values are annotated for response-body serialization. (`src/Light.Results.AspNetCore.Shared/Serialization/DefaultResultJsonConverter.cs`)

**MetadataValueAnnotation Semantics**
- Plan: default annotation is `None`, and `None` means “use the serialization mode”; `SerializeInBoth` is the flag name. Actual: default annotation is `SerializeInHttpResponseBody`, `None` means “do not serialize,” and the combined flag name is `SerializeInHttpHeaderAndBody`. (`src/Light.Results/Metadata/MetadataValueAnnotation.cs`, `src/Light.Results/Metadata/MetadataValue.cs`)
- Plan: per-value annotations drive inclusion in body vs headers per the matrix in the plan. Actual: body serialization now filters metadata entries/array elements by `SerializeInHttpResponseBody`, but because the default annotation is already `SerializeInHttpResponseBody`, values participate in body serialization unless explicitly annotated otherwise. (`src/Light.Results.AspNetCore.Shared/Serialization/MetadataValueJsonConverter.cs`, `src/Light.Results/Metadata/MetadataValue.cs`)
- Plan: `SerializeInHttpHeader` and `SerializeInBoth` should both emit headers. Actual: header emission now correctly checks the flag via `HasAnnotation(SerializeInHttpHeader)`, so both `SerializeInHttpHeader` and `SerializeInHttpHeaderAndBody` emit headers, while `None` does not. (`src/Light.Results.AspNetCore.Shared/HttpExtensions.cs`, `src/Light.Results/Metadata/MetadataValueExtensions.cs`)

**Header Conversion Behavior**
- Plan: header names use a default X-PascalCase strategy with well-known mappings (ETag, Last-Modified), and arrays serialize as comma-separated values. Actual: headers are produced by `IHttpHeaderConversionService` with optional `HttpHeaderConverter`s per metadata key; the default uses the metadata key verbatim as the header name and `MetadataValue.ToString()` for the value (JSON-like formatting, arrays as `[...]`). (`src/Light.Results/Http/IHttpHeaderConversionService.cs`, `src/Light.Results/Http/DefaultHttpHeaderConversionService.cs`, `src/Light.Results/Metadata/MetadataValue.cs`)
- Plan: `Null` metadata values should omit headers. Actual: null-valued metadata entries are now skipped when writing HTTP headers. (`src/Light.Results.AspNetCore.Shared/HttpExtensions.cs`)

**OpenAPI Schema Generation**
- Plan: `LightResult`/`LightResult<T>` implement `IResultTypeMetadata<T>` in addition to the `ProducesLightResult` extension. Actual: OpenAPI metadata is only supplied via `ProducesLightResult` extensions using the schema-only `WrappedResponse<TValue, TMetadata>` type; `LightResult` does not implement `IResultTypeMetadata<T>`. (`src/Light.Results.AspNetCore.MinimalApis/LightResultEndpointExtensions.cs`, `src/Light.Results.AspNetCore.MinimalApis/WrappedResponse.cs`)

**Additional Design Additions Not In The Plan**
- A generic `IHttpResultEnricher` and the `ICanReplaceMetadata` interface enable DI-driven metadata enrichment before header/body serialization. (`src/Light.Results.AspNetCore.Shared/Enrichment/IHttpResultEnricher.cs`, `src/Light.Results/Metadata/ICanReplaceMetadata.cs`, `src/Light.Results.AspNetCore.MinimalApis/BaseLightResult.cs`)
