# Make the Native AOT Compatibility Claim Verifiable

## Rationale

`Light.PortableResults` advertises "Compatible with .NET Native AOT" in its package description, and the README repeats the claim for the base, validation, and Minimal APIs packages. The base package never sets `IsAotCompatible`, so the trim and AOT analyzers have never run over it. Enabling them for the `net10.0` asset surfaces 68 IL2026/IL3050 warnings, all in the CloudEvents write path and the two read paths.

The warnings are not cosmetic. Both `PortableResultsCloudEventsWriteOptions.Default` and `PortableResultsHttpReadOptions.Default` build a `JsonSerializerOptions` with converters but no `TypeInfoResolver`. Under `JsonSerializerIsReflectionEnabledByDefault=false`, the switch Native AOT sets, `result.ToCloudEvent(...)`, `Result.Ok().ToCloudEvent(...)`, and `response.ReadResultAsync()` all throw `InvalidOperationException: Reflection-based serialization has been disabled for this application`. The first two are the README's "Publish to RabbitMQ" quick start, and the second involves no user type at all. Because the annotations are never propagated, the consumer gets no compile-time warning and discovers this when the AOT binary throws.

The underlying design is sound: registering `CloudEventsEnvelopeForWriting`, `CloudEventsEnvelopeForWriting<T>`, and the `HttpRead*Payload` types in a `JsonSerializerContext` makes every one of those calls succeed. What is missing is the plumbing that makes that enforceable, discoverable, and regression-proof. Do this before 0.7.0: the corrective work touches the public surface and is cheap while breaking changes are still allowed.

## Acceptance Criteria

- [ ] The `net10.0` assets of `Light.PortableResults` and `Light.PortableResults.Validation` build with `IsAotCompatible`, and the `netstandard2.0` assets build without `NETSDK1210`.
- [ ] Both packages build clean under `Release`, where `TreatWarningsAsErrors` turns any future IL2026/IL3050 into a build failure. No warning is resolved by disabling a rule in `.editorconfig` or `NoWarn`.
- [ ] The core package ships public source-generated `JsonSerializerContext` types covering its own CloudEvents write, CloudEvents read, and HTTP read payload types, so a consumer never has to name library-internal payload types in their own context.
- [ ] Options constructed from the shipped contexts round-trip a non-generic `Result` and a `Result<T>` over CloudEvents and HTTP without a reflection-based resolver present, proving the paths need no runtime code generation. Tests assert this in-process by resolver composition, not by toggling the reflection switch.
- [ ] Behavior for consumers who pass reflection-backed options is unchanged: the existing test suites pass without modification beyond additions, and `PortableResultsCloudEventsWriteOptions.Default` and `PortableResultsHttpReadOptions.Default` keep serializing arbitrary `T` as they do today.
- [ ] The Native AOT sample exercises the CloudEvents write path and the HTTP read path, so ILC covers the code this issue is about, and its blanket `SuppressTrimAnalysisWarnings` no longer hides warnings from Light.PortableResults assemblies.
- [ ] CI publishes the sample with `PublishAot=true` and fails on any trim or AOT warning originating in a Light.PortableResults assembly.
- [ ] The README documents how to compose options for Native AOT, and the `<PackageReleaseNotes>` of every affected package describe their respective changes.

## Technical Details

### Enabling the analyzers

`IsAotCompatible` is unsupported on `netstandard2.0` and raises `NETSDK1210` when set unconditionally. Use the SDK's own recommended form in both multi-targeted projects:

```xml
<IsAotCompatible Condition="$([MSBuild]::IsTargetFrameworkCompatible('$(TargetFramework)', 'net8.0'))">true</IsAotCompatible>
```

This is independent of the existing `PublishAot=false` and `TreatAsLocalProperty="PublishAot"` settings, which stay as they are. `Light.PortableResults.Validation` is clean today; enabling it there buys regression protection only, at no triage cost.

Measured warning distribution in `Light.PortableResults` before any fix — use it to confirm the triage is complete:

| File | IL2026 | IL3050 |
| --- | ---: | ---: |
| `CloudEvents/Reading/ReadOnlyMemoryCloudEventsExtensions.cs` | 16 | 16 |
| `Http/Reading/HttpResponseMessageExtensions.cs` | 12 | 12 |
| `CloudEvents/Writing/CloudEventsResultExtensions.cs` | 4 | 4 |
| `Http/Reading/Json/ResultJsonReader.cs` | 2 | 2 |

### Resolving the warnings: route through `JsonTypeInfo`, do not annotate

Every warning is a call to a `JsonSerializer.Serialize`/`Deserialize<T>(…, JsonSerializerOptions)` overload. The payload type is statically known at each site; only the resolver is the caller's. The library already has the correct pattern for this and it produces no warnings: `SystemTextJsonWritingExtensions` resolves `options.GetTypeInfo(typeof(T))`, casts to `JsonTypeInfo<T>`, and calls the `JsonTypeInfo` overload; `LightResult` and `LightActionResult` do the same and throw an actionable message when the cast fails. Apply that pattern to the four files above.

This is the decision that shapes the rest of the work, so record why the alternative was rejected. Annotating the public entry points with `[RequiresDynamicCode]`/`[RequiresUnreferencedCode]` would be permanently wrong: these methods do not intrinsically require dynamic code, they require the caller's options to carry a resolver. Annotating would force a suppression on every correctly configured AOT consumer while doing nothing for an incorrectly configured one. Blanket `UnconditionalSuppressMessage` at the call sites is equally wrong here — it silences a diagnostic that is currently accurate. The two existing suppressions in `CloudEventsEnvelopeForWritingJsonConverterFactory` and its HTTP counterpart stay: `MakeGenericType` over a resolved generic is genuinely safe when the closed type is registered, which is what their justifications already state.

Where a resolved `JsonTypeInfo<T>` cannot be obtained, throw `InvalidOperationException` naming the missing type and pointing at the shipped context, matching the wording style already used in `LightResult`. This replaces `System.Text.Json`'s generic "reflection has been disabled" message, which does not tell the caller which type to register.

### Shipped contexts and the default options

Ship public `JsonSerializerContext` types in the core package, one per direction, mirroring `PortableResultsMinimalApiJsonContext`: the CloudEvents write envelopes, the CloudEvents read payloads, and the HTTP read payloads, plus `MetadataObject` and `MetadataValue`. `GenerationMode = JsonSourceGenerationMode.Metadata` is the right mode; serialization mode buys nothing for converter-backed types. Verify the S.T.J. source generator behaves on the `netstandard2.0` asset — if it does not, condition the contexts to the `net10.0` asset, since Native AOT only exists there.

Leave `PortableResultsCloudEventsWriteOptions.Default` and `PortableResultsHttpReadOptions.Default` reflection-backed. They cannot be made AOT-complete regardless: a generic `Result<T>` needs `CloudEventsEnvelopeForWriting<T>` or `HttpReadAutoSuccessResultPayload<T>` closed over the consumer's own type, which only the consumer's context can supply. Setting a source-generated resolver on the shared defaults would silently stop arbitrary `T` from serializing for the majority of consumers who do not use AOT — a far worse trade than requiring explicit opt-in. Instead, expose the composition explicitly, for example a `Create…SerializerOptions(IJsonTypeInfoResolver consumerResolver)` helper per direction that combines the shipped context with the consumer's via `JsonTypeInfoResolver.Combine` and applies the existing `AddDefault…Converters` call. Consumers who prefer to wire it by hand can keep doing so; the helper exists so the README has one line to point at.

Confirmed working shape, for reference — this succeeds today with reflection disabled, without any library change:

```csharp
var options = new JsonSerializerOptions(JsonSerializerDefaults.Web) { TypeInfoResolver = MyContext.Default };
options.AddDefaultPortableResultsCloudEventsWriteJsonConverters();
```

where `MyContext` declares `[JsonSerializable(typeof(CloudEventsEnvelopeForWriting))]` and `[JsonSerializable(typeof(CloudEventsEnvelopeForWriting<string>))]`. The work here is to make the library half of that automatic.

### Proving it, in tests and in CI

Two layers, because neither is sufficient alone.

In-process tests compose options whose only resolver is a source-generated context — the shipped one combined with a test-local context for the value type — and assert that a non-generic `Result` and a `Result<T>` round-trip over CloudEvents and HTTP. A path that completes with no reflection-based resolver in the graph cannot have needed runtime code generation, which is the property under test. Do not toggle `System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault` from a test: the value is read once and cached per process, and the test hosts are shared and parallel. The switch remains the right manual reproduction, via a standalone console app with `<JsonSerializerIsReflectionEnabledByDefault>false</JsonSerializerIsReflectionEnabledByDefault>`, and that is how the failures in the Rationale were confirmed.

The ILC publish is the end-to-end gate, and it only means something once two things change in `samples/NativeAotMovieRating`. First, the sample is a server that exercises the Minimal API write path exclusively — the path that already works. Extend it to publish a CloudEvents message and to read a `Result<T>` back from an `HttpResponseMessage`, so ILC and the trim analyzer actually walk the code this issue is about; the in-memory database module is a reasonable place to hang an outbox-style publish, and the sample's own endpoints can serve as the source for a typed-client read. Second, the sample sets `SuppressTrimAnalysisWarnings=true` to silence Serilog's unannotated assembly rollup, which would equally hide every warning this work is meant to catch. Replace it with a targeted suppression scoped to the Serilog assemblies, and set `TrimmerSingleWarn=false` so individual warnings are reported rather than collapsed into one per assembly.

Add the publish to `build-and-test.yml` as a separate step after the existing test steps, on the runner's own RID. It is the slowest step in the workflow; keep it out of the matrix and do not gate the coverage jobs on it.

### Deliberately out of scope

- **`Light.PortableResults.AspNetCore.Mvc`.** MVC is not Native AOT compatible, and its package description does not claim otherwise.
- **Making the shared `Default` options AOT-complete.** Rejected above; the generic payload types make it impossible without the consumer's context.
- **Trim-only (`PublishTrimmed`) verification as a separate gate.** The AOT publish subsumes the trim analyzer for this code; a dedicated trimmed-but-not-AOT configuration would add a second slow CI step for no additional signal.
- **The `default(Result<T>)` write guard** and **`EnablePackageValidation`.** Tracked separately as items 2 and 3 of #77.
