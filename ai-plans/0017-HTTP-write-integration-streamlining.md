# HTTP Write Integration Streamlining

## Rationale

The HTTP writing integration currently bypasses the standard System.Text.Json (STJ) pipeline in the ASP.NET Core Minimal API and MVC layers. Both `LightResult` / `LightResult<T>` (Minimal APIs) and `LightActionResult` / `LightActionResult<T>` (MVC) attempt to downcast the resolved `JsonConverter` to `HttpWriteResultJsonConverter` or `HttpWriteResultJsonConverter<T>` and then call a custom `Serialize` method that accepts `LightResultsHttpWriteOptions` as an additional argument. This design has two key problems:

1. **Callers cannot replace converters.** Because the ASP.NET Core result types check for a specific converter class via downcast, registering a custom `JsonConverter<Result>` or `JsonConverter<Result<T>>` will only be used when the downcast fails (the fallback path), and in that case the `LightResultsHttpWriteOptions` are silently ignored — producing incorrect output.

2. **Options are passed out-of-band.** `HttpWriteResultJsonConverter` receives `LightResultsHttpWriteOptions` both at construction time (for the `Write` override) and as an extra argument through the `Serialize` method (for per-request overrides). This dual-channel design is fragile and unlike the CloudEvents writing feature, which solved the same problem cleanly by embedding resolved options into an intermediary struct that flows through the normal STJ pipeline.

This plan adopts the CloudEvents pattern: introduce intermediary **wrapper structs** (`HttpResultForWriting` / `HttpResultForWriting<T>`) that bundle the `Result` together with resolved serialization options. Stateless JSON converters serialize these wrapper types through the standard STJ `JsonSerializer.Serialize` call. The ASP.NET Core layers (both Minimal APIs and MVC) construct the wrapper and hand it to STJ — no downcasts, no special `Serialize` methods.

## Acceptance Criteria

### Intermediary Types

- [ ] A `ResolvedHttpWriteOptions` readonly record struct captures the frozen, per-request serialization options (derived from `LightResultsHttpWriteOptions` and any per-call overrides).
- [ ] A public extension method `ToResolvedHttpWriteOptions()` on `LightResultsHttpWriteOptions` creates `ResolvedHttpWriteOptions`. This is the single public conversion point used by both the base classes and the wrapper construction.
- [ ] `HttpResultForWriting` (non-generic, wrapping `Result`) and `HttpResultForWriting<T>` (wrapping `Result<T>`) readonly record structs carry the result data together with `ResolvedHttpWriteOptions`.
- [ ] Extension methods `ToHttpResultForWriting` on `Result` and `Result<T>` construct these wrapper structs.

### JSON Converters for Intermediary Types

- [ ] `HttpResultForWritingJsonConverter` serializes `HttpResultForWriting` by writing either an empty body / metadata-only object (success) or Problem Details (failure) — reusing existing serialization helpers.
- [ ] `HttpResultForWritingJsonConverter<T>` serializes `HttpResultForWriting<T>` by writing the value (possibly wrapped with metadata) on success, or Problem Details on failure.
- [ ] `HttpResultForWritingJsonConverterFactory` creates generic converters for any `HttpResultForWriting<T>`.
- [ ] All converters are **stateless** (no constructor parameters) and **write-only** (`Read` throws `NotSupportedException`).
- [ ] Converters access serialization options from the wrapper struct directly, not from ambient state.

### ASP.NET Core Minimal API Integration

- [ ] `LightResult.WriteBodyAsync` and `LightResult<T>.WriteBodyAsync` construct the `HttpResultForWriting` / `HttpResultForWriting<T>` wrapper, look up the `JsonTypeInfo` for it, and call `JsonSerializer.Serialize` — no downcast to a specific converter class.
- [ ] The `Serialize` shortcut methods and the converter downcast branches are removed from `LightResult` and `LightResult<T>`.

### ASP.NET Core MVC Integration

- [ ] `LightActionResult.WriteBodyAsync` and `LightActionResult<T>.WriteBodyAsync` follow the same wrapper pattern, matching the Minimal API changes.
- [ ] The converter downcast branches are removed from `LightActionResult` and `LightActionResult<T>`.

### Shared Base Classes

- [ ] `BaseLightResult<TResult>` and `BaseLightActionResult<TResult>` keep `LightResultsHttpWriteOptions?` as the constructor parameter and stored property (it remains the user-facing type).
- [ ] `ExecuteAsync` / `ExecuteResultAsync` resolves options once at the top: merge the stored `OverrideOptions` with DI-resolved options (via `ResolveLightResultOptions`), then freeze to `ResolvedHttpWriteOptions` via `ToResolvedHttpWriteOptions()`. The single `ResolvedHttpWriteOptions` is passed to both `SetHeaders` and `WriteBodyAsync`.
- [ ] `SetHeaders` and `WriteBodyAsync` signatures change to accept `ResolvedHttpWriteOptions` so they no longer resolve options themselves.

### DI / Module Registration

- [ ] `AddDefaultLightResultsHttpWriteJsonConverters` registers the new converters (`HttpResultForWritingJsonConverter`, `HttpResultForWritingJsonConverterFactory`) instead of the current `HttpWriteResultJsonConverter` and `HttpWriteResultJsonConverterFactory`. The metadata converters (`HttpWriteMetadataObjectJsonConverter`, `HttpWriteMetadataValueJsonConverter`) remain.
- [ ] The registration method no longer requires `LightResultsHttpWriteOptions` as a parameter because the new converters are stateless. Update `Module` in both Minimal APIs and MVC accordingly (the `Configure<LightResultsHttpWriteOptions>` lambda for JSON options becomes simpler or unnecessary).

### Code Cleanup

- [ ] `HttpWriteResultJsonConverter`, `HttpWriteResultJsonConverter<T>`, and `HttpWriteResultJsonConverterFactory` are removed.
- [ ] The `Serialize` method overloads that accepted `LightResultsHttpWriteOptions` are removed.
- [ ] Existing tests pass with no regressions.
- [ ] New tests verify `HttpResultForWriting` / `HttpResultForWriting<T>` construction and serialization.

### Benchmarks

- [ ] Existing HTTP serialization benchmarks are updated to use the new intermediary types and run to verify no significant performance regression.

## Technical Details

### Intermediary Types

Place these in `src/Light.Results/Http/Writing/`:

#### `ResolvedHttpWriteOptions`

```csharp
public readonly record struct ResolvedHttpWriteOptions(
    ValidationProblemSerializationFormat ValidationProblemSerializationFormat,
    MetadataSerializationMode MetadataSerializationMode,
    Func<Errors, MetadataObject?, ProblemDetailsInfo>? CreateProblemDetailsInfo,
    bool FirstErrorCategoryIsLeadingCategory
);
```

This struct is a frozen snapshot of `LightResultsHttpWriteOptions`, created once per request. It avoids repeatedly reading from the mutable options class during serialization. A public extension method provides the conversion:

```csharp
public static ResolvedHttpWriteOptions ToResolvedHttpWriteOptions(
    this LightResultsHttpWriteOptions options
) => new (
    options.ValidationProblemSerializationFormat,
    options.MetadataSerializationMode,
    options.CreateProblemDetailsInfo,
    options.FirstErrorCategoryIsLeadingCategory
);
```

This is the single point of truth for converting mutable options to frozen options. It is called once at the top of `ExecuteAsync` / `ExecuteResultAsync` (after resolving the final `LightResultsHttpWriteOptions` from DI or override) and by the `ToHttpResultForWriting` extension methods.

#### Wrapper Types: `HttpResultForWriting` and `HttpResultForWriting<T>`

```csharp
public readonly record struct HttpResultForWriting(
    Result Data,
    ResolvedHttpWriteOptions ResolvedOptions
);

public readonly record struct HttpResultForWriting<T>(
    Result<T> Data,
    ResolvedHttpWriteOptions ResolvedOptions
);
```

#### Extension Methods

An `HttpResultForWritingExtensions` static class provides:

```csharp
public static HttpResultForWriting ToHttpResultForWriting(
    this Result result,
    LightResultsHttpWriteOptions options
);

public static HttpResultForWriting<T> ToHttpResultForWriting<T>(
    this Result<T> result,
    LightResultsHttpWriteOptions options
);
```

These methods create `ResolvedHttpWriteOptions` from the supplied `LightResultsHttpWriteOptions` and return the wrapper struct.

### Converter Architecture

Place converters in `src/Light.Results/Http/Writing/Json/`:

```
Http/Writing/
├── HttpResultForWriting.cs
├── HttpResultForWritingExtensions.cs
├── ResolvedHttpWriteOptions.cs
├── Json/
│   ├── HttpResultForWritingJsonConverter.cs      (non-generic + generic + factory)
│   ├── HttpWriteMetadataObjectJsonConverter.cs   (unchanged)
│   └── HttpWriteMetadataValueJsonConverter.cs    (unchanged)
```

The converters reuse the existing `SerializerExtensions.SerializeProblemDetailsAndMetadata` and `WriteMetadataPropertyAndValue` helpers. The key difference is that the converter reads options from the wrapper's `ResolvedOptions` field instead of from a constructor-injected `LightResultsHttpWriteOptions` instance.

Because `SerializeProblemDetailsAndMetadata` currently accepts `LightResultsHttpWriteOptions`, it needs a new overload (or refactor) to accept `ResolvedHttpWriteOptions`. Keep the existing overload if backwards compatibility for direct callers is desired, or replace it since the library is not published yet and breaking changes are allowed.

### ASP.NET Core Integration Changes

#### Base Class Changes

`BaseLightResult<TResult>` and `BaseLightActionResult<TResult>` keep `LightResultsHttpWriteOptions?` as the constructor parameter and `OverrideOptions` property — this is the user-facing type that callers pass to `ToMinimalApiResult`, `ToMvcActionResult`, etc.

The change happens inside `ExecuteAsync` / `ExecuteResultAsync`:

1. Resolve the final `LightResultsHttpWriteOptions` via the existing `ResolveLightResultOptions(OverrideOptions)` path (override ?? DI).
2. Freeze to `ResolvedHttpWriteOptions` by calling `ToResolvedHttpWriteOptions()` — once.
3. Pass the single `ResolvedHttpWriteOptions` to both `SetHeaders` and `WriteBodyAsync`.

`SetHeaders` signature changes to accept `ResolvedHttpWriteOptions` instead of reading options from the `HttpContext` internally. It uses the struct's fields directly for status code, content type, and metadata header decisions.

`WriteBodyAsync` signature changes to also receive `ResolvedHttpWriteOptions` so subclasses can construct the envelope without any additional resolution.

#### `LightResult.WriteBodyAsync` (Minimal APIs)

The method receives the already-resolved `ResolvedHttpWriteOptions` from the base class and becomes:

1. Resolve `JsonSerializerOptions` (existing logic via `ResolveJsonSerializerOptions`).
2. Construct `HttpResultForWriting` from the enriched result and the received `ResolvedHttpWriteOptions`.
3. Look up `JsonTypeInfo` for `HttpResultForWriting` from the serializer options.
4. Serialize with `JsonSerializer.Serialize(writer, wrapper, typeInfo)`.

The same applies to `LightResult<T>.WriteBodyAsync` with `HttpResultForWriting<T>`.

#### `LightActionResult.WriteBodyAsync` (MVC)

Identical pattern, using `ResolveMvcJsonSerializerOptions` instead.

#### Extension Methods (`ToMinimalApiResult`, `ToMvcActionResult`)

These public extension methods remain unchanged — they accept `LightResultsHttpWriteOptions?` and pass it straight through to the constructor. No conversion happens here; freezing to `ResolvedHttpWriteOptions` is deferred to `ExecuteAsync` / `ExecuteResultAsync`.

### Module Registration

`AddDefaultLightResultsHttpWriteJsonConverters` currently requires `LightResultsHttpWriteOptions` and creates converters with it:

```csharp
// Current
serializerOptions.Converters.Add(new HttpWriteResultJsonConverter(options));
serializerOptions.Converters.Add(new HttpWriteResultJsonConverterFactory(options));
```

After the change:

```csharp
// New
serializerOptions.Converters.Add(new HttpResultForWritingJsonConverter());
serializerOptions.Converters.Add(new HttpResultForWritingJsonConverterFactory());
```

The `AddDefaultLightResultsHttpWriteJsonConverters` method signature changes to no longer require `LightResultsHttpWriteOptions`. This simplifies the `Module.ConfigureMinimalApiJsonOptionsForLightResults` and `Module.ConfigureMvcJsonOptionsForLightResults` methods: they no longer need the `Configure<LightResultsHttpWriteOptions>` lambda to inject options into the JSON setup.

### Non-Generic `Result` Body Writing Edge Case

When `Result` is valid and has no metadata to serialize, the current converter writes nothing at all (empty body). The new converter should preserve this behavior: if the result is valid and there is no metadata to write, `HttpResultForWritingJsonConverter` should write nothing. This requires the ASP.NET Core layer to check before calling `JsonSerializer.Serialize` — or the converter can write nothing and the caller omits the content type (which is already handled by `SetContentTypeFromResult`). Note: the current `SetContentTypeFromResult` already skips setting a content type for this case. The converter should simply not be invoked when there is nothing to write — this check belongs in `WriteBodyAsync`.

### Performance Considerations

1. All intermediary types are readonly record structs — no heap allocations for the wrapper itself.
2. `ResolvedHttpWriteOptions` is small (fits in a few machine words) — minimal copy overhead.
3. Options are resolved and frozen once at the top of `ExecuteAsync` / `ExecuteResultAsync`. The single `ResolvedHttpWriteOptions` instance is reused for headers and body writing.
4. STJ converter resolution adds minor overhead compared to the current direct downcast, but enables full pipeline extensibility — the same trade-off accepted for CloudEvents.
