# Make the Native AOT Compatibility Claim Verifiable

## Rationale

`Light.PortableResults` advertises Native AOT compatibility, but its `net10.0` asset does not set `IsAotCompatible`. Enabling the trim and AOT analyzers exposes 68 IL2026/IL3050 warnings across CloudEvents writing and the CloudEvents and HTTP read paths.

The warnings represent runtime failures: with reflection serialization disabled, the default options cannot resolve library-owned envelope and payload types, so even non-generic operations throw. The library already owns converters for those types and should supply their contracts while leaving consumers responsible only for their result value types. Make that behavior enforceable and regression-protected before 0.7.0: the corrective work changes public surface, which is cheap while breaking changes are still allowed and expensive afterwards.

## Acceptance Criteria

- [ ] The `net10.0` assets of `Light.PortableResults` and `Light.PortableResults.Validation` build with `IsAotCompatible`; their `netstandard2.0` assets build without `NETSDK1210`.
- [ ] Both packages build clean in `Release`, where future IL2026/IL3050 diagnostics fail the build. No rule is disabled through `.editorconfig` or `NoWarn`.
- [ ] Consumers register only their result value types in `JsonSerializerContext`; the library resolves its own CloudEvents write, CloudEvents read, and HTTP read types.
- [ ] With a source-generated resolver declaring only the value type, non-generic `Result` and generic `Result<T>` round-trip over CloudEvents and HTTP in process without a reflection-backed resolver.
- [ ] Library-owned contracts are created once per `JsonSerializerOptions` instance and can be created after the options become read-only.
- [ ] Missing consumer value-type metadata produces an exception that names the type and remedy at every affected entry point, with a negative test per site. The unreachable guards in `SystemTextJsonWritingExtensions`, `LightResult`, and `LightActionResult` are repaired.
- [ ] Reflection-backed behavior remains unchanged: existing tests require additions only, the shared default options continue to support arbitrary `T`, and converters configured ahead of the library defaults retain precedence. Library-created contracts are used only when the configured resolver cannot supply one.
- [ ] CI publishes the Native AOT sample, fails on trim or AOT diagnostics from Light.PortableResults assemblies, and keeps diagnostics from unannotated package dependencies non-fatal. The replacement for `SuppressTrimAnalysisWarnings` is proven with an injected library-side regression.
- [ ] The README documents Native AOT option composition, and every affected package updates `<PackageReleaseNotes>`.

## Technical Details

### Analyzer configuration and warning triage

Set `IsAotCompatible` only on compatible target frameworks in both multi-targeted projects:

```xml
<IsAotCompatible Condition="$([MSBuild]::IsTargetFrameworkCompatible('$(TargetFramework)', 'net8.0'))">true</IsAotCompatible>
```

Keep the existing `PublishAot=false` and `TreatAsLocalProperty="PublishAot"` settings. `Light.PortableResults.Validation` is currently clean; enabling its analyzers provides regression protection.

Use the measured baseline to confirm complete triage:

| File | IL2026 | IL3050 |
| --- | ---: | ---: |
| `CloudEvents/Reading/ReadOnlyMemoryCloudEventsExtensions.cs` | 16 | 16 |
| `Http/Reading/HttpResponseMessageExtensions.cs` | 12 | 12 |
| `CloudEvents/Writing/CloudEventsResultExtensions.cs` | 4 | 4 |
| `Http/Reading/Json/ResultJsonReader.cs` | 2 | 2 |

Route each warned serialization call through a `JsonTypeInfo<T>` overload. Do not annotate public entry points with `[RequiresDynamicCode]` or `[RequiresUnreferencedCode]`: the operations require a configured resolver, not intrinsic dynamic code. Do not suppress these call sites either. The four existing, justified `MakeGenericType` factory suppressions remain unchanged.

### Contract resolution and error behavior

Use `JsonSerializerOptions.TryGetTypeInfo`; `GetTypeInfo` throws `NotSupportedException` for a missing contract before a null or failed-cast guard can run. Convert the existing unreachable guards in `SystemTextJsonWritingExtensions`, `LightResult`, and `LightActionResult`, and use the same pattern at new sites. A failed lookup becomes `InvalidOperationException` naming the unresolved consumer type and instructing the caller to register it in the context supplied to the options. Negative tests assert the exception type and unresolved type at every converted site.

Combining source-generated resolvers cannot synthesize a closed contract such as `CloudEventsEnvelopeForWriting<MyDto>` from separate contracts for the wrapper and `MyDto`. Do not ship library contexts that force consumers to declare closed library wrappers. Instead, resolve every library-owned wrapper in this order:

1. If `options.TypeInfoResolver` is null and `JsonSerializer.IsReflectionEnabledByDefault` is true, call `options.MakeReadOnly(populateMissingResolver: true)`.
2. Use a typed contract returned by `options.TryGetTypeInfo(typeof(TWrapper), ...)` when available.
3. Otherwise, select a registered converter using normal `JsonSerializerOptions` precedence, including replacements inserted before the defaults; fall back to the library converter only when none matches. Cache the resulting library-owned contract or converter per `JsonSerializerOptions` instance and closed wrapper type.

The ordering is load-bearing, not a preference: `HttpResponseMessageExtensionsTests` inserts `NullBareStringPayloadConverter` and `EmptyFailurePayloadConverter` at index 0 and asserts the read path uses them, so reaching for a library-created contract first fails those tests.

Step 1 preserves current reflection-backed behavior: default options contain converters but no explicit resolver, and `TryGetTypeInfo` cannot expose their contracts until the reflection resolver is materialized. `MakeReadOnly(bool)` carries IL2026 and IL3050; narrowly suppress both at this guarded call because `JsonSerializer.IsReflectionEnabledByDefault` is a link-time constant and makes the call unreachable when reflection is disabled. Freezing here is not an additional behavior change because serialization would freeze the options immediately afterward.

For read fallbacks, build the cached contract with `JsonMetadataServices.CreateValueInfo<TWrapper>(options, converter)` and use the `JsonTypeInfo` serializer overload. Contract creation is reflection-free, analyzer-clean, and works with read-only options. For CloudEvents write fallback, invoke the selected converter directly; the library converter already delegates to `writer.WriteCloudEvents(envelope, options.SerializerOptions)`. This preserves custom converter precedence without resolving a wrapper contract.

With reflection-backed options, step 2 preserves existing behavior. With a source-generated resolver that declares only the consumer's `T`, wrapper lookup fails and step 3 handles the library type; nested value serialization still resolves `T` from the consumer context. The shared defaults remain reflection-backed so arbitrary consumer types continue to work, and non-generic results should then require no consumer registration at all — confirm that during implementation rather than assuming it, and document the outcome in the README.

All four factory types and their suppressions remain public and unchanged: the CloudEvents write, CloudEvents read, and HTTP read factories continue serving resolver-backed contracts, while the unrelated HTTP write factory is unaffected.

### Tests and CI

In-process tests use options whose only resolver is a test-local source-generated context declaring the value type. Round-trip non-generic and generic results over CloudEvents and HTTP, verify configured replacement converters still win, verify contract caching and read-only options, and cover the missing-type errors. Do not toggle `JsonSerializer.IsReflectionEnabledByDefault` inside the shared parallel test process; standalone reproduction may use `<JsonSerializerIsReflectionEnabledByDefault>false</JsonSerializerIsReflectionEnabledByDefault>`.

The Native AOT sample currently exercises only the already-working Minimal API write path. Its publish remains an ILC regression gate for this change; extending it to CloudEvents write and HTTP read is deferred to a maintainer-driven follow-up.

Remove `SuppressTrimAnalysisWarnings=true` from the sample and retain single-warning mode for package references:

```xml
<!-- Keep package-reference rollups non-fatal; project references still report detailed diagnostics. -->
<WarningsNotAsErrors>$(WarningsNotAsErrors);IL2104</WarningsNotAsErrors>
```

Do not set `TrimmerSingleWarn=false`: it expands Serilog to IL2067, which Release promotes to an error, while globally suppressing IL2067 could hide the same defect in Light assemblies. The proposed configuration was verified by injecting a reflection-based serialization call into `Light.PortableResults`: Serilog remained a non-fatal IL2104 rollup, while the Light project reference emitted fatal IL2026/IL3050 diagnostics with source locations.

Add a separate post-test step to `build-and-test.yml` that publishes the sample on the runner RID:

```shell
dotnet publish <sample-project> -c Release -r <rid>
```

Do not pass `/p:PublishAot=true`: as a global property it reaches project references and causes `NETSDK1207` in the `netstandard2.0` source generator. The sample already sets `PublishAot` and removes it from project references through `GlobalPropertiesToRemove`. Keep this slow publish outside any matrix and do not make coverage jobs depend on it.

Deliberately out of scope:

- Native AOT support for `Light.PortableResults.AspNetCore.Mvc`; only its shared unreachable guard is repaired.
- A source-generated resolver on the shared default options.
- Shipped `JsonSerializerContext` types for library-owned payloads.
- Extending the Native AOT sample to exercise CloudEvents write and HTTP read.
- A separate trim-only publish gate.
- The `default(Result<T>)` write guard and `EnablePackageValidation`, tracked as items 2 and 3 of #77.
