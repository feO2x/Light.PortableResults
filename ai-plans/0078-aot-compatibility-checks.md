# Make the Native AOT Compatibility Claim Verifiable

## Rationale

`Light.PortableResults` advertises "Compatible with .NET Native AOT" in its package description, and the README repeats the claim for the base, validation, and Minimal APIs packages. The base package never sets `IsAotCompatible`, so the trim and AOT analyzers have never run over it. Enabling them for the `net10.0` asset surfaces 68 IL2026/IL3050 warnings, all in the CloudEvents write path and the two read paths.

The warnings are not cosmetic. Both `PortableResultsCloudEventsWriteOptions.Default` and `PortableResultsHttpReadOptions.Default` build a `JsonSerializerOptions` with converters but no `TypeInfoResolver`. Under `JsonSerializerIsReflectionEnabledByDefault=false`, the switch Native AOT sets, `result.ToCloudEvent(...)`, `Result.Ok().ToCloudEvent(...)`, and `response.ReadResultAsync()` all throw `InvalidOperationException: Reflection-based serialization has been disabled for this application`. The first two are the README's "Publish to RabbitMQ" quick start, and the second involves no user type at all. Because the annotations are never propagated, the consumer gets no compile-time warning and discovers this when the AOT binary throws.

The underlying design is sound. Every failing site serializes a library-owned type through a library-owned converter, so the library can supply the contract itself and leave the consumer responsible for their own value type alone. What is missing is the plumbing that makes that so, enforceably and regression-proof. Do this before 0.7.0: the corrective work touches the public surface and is cheap while breaking changes are still allowed.

## Acceptance Criteria

- [ ] The `net10.0` assets of `Light.PortableResults` and `Light.PortableResults.Validation` build with `IsAotCompatible`, and the `netstandard2.0` assets build without `NETSDK1210`.
- [ ] Both packages build clean under `Release`, where `TreatWarningsAsErrors` turns any future IL2026/IL3050 into a build failure. No warning is resolved by disabling a rule in `.editorconfig` or `NoWarn`.
- [ ] A consumer never names a library-owned type in their own `JsonSerializerContext`. Registering the result value type is sufficient for CloudEvents write, CloudEvents read, and HTTP read; the library resolves contracts for its own envelope and payload types itself.
- [ ] A non-generic `Result` and a `Result<T>` round-trip over CloudEvents and HTTP against options whose only resolver is a consumer context declaring the value type alone, proving the paths need no runtime code generation. Tests assert this in-process by resolver composition, not by toggling the reflection switch.
- [ ] Library-owned contracts are created once per `JsonSerializerOptions` instance, not per serialization call, and creation works against options that are already read-only.
- [ ] Behavior for consumers who pass reflection-backed options is unchanged: the existing test suites pass without modification beyond additions, and `PortableResultsCloudEventsWriteOptions.Default` and `PortableResultsHttpReadOptions.Default` keep serializing arbitrary `T` as they do today.
- [ ] CI publishes the Native AOT sample with `PublishAot=true` and fails on any trim or AOT warning originating in a Light.PortableResults assembly, which requires that the sample's blanket `SuppressTrimAnalysisWarnings` no longer hide them.
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

Where a resolved `JsonTypeInfo<T>` cannot be obtained for the consumer's value type, throw `InvalidOperationException` naming the missing type, matching the wording style already used in `LightResult`. This replaces `System.Text.Json`'s generic "reflection has been disabled" message, which does not tell the caller which type to register.

### Library-owned contracts, not resolver composition

Resolver composition cannot carry this. `JsonTypeInfoResolver.Combine` queries each resolver in order and returns the first contract that matches the requested type; it never synthesizes `CloudEventsEnvelopeForWriting<MyDto>` from a contract for the envelope and another for `MyDto`. Verified: with a consumer context declaring only `string`, the current write path throws `NotSupportedException` for `CloudEventsEnvelopeForWriting<string>`. Shipping library contexts and combining them would therefore not spare consumers from declaring every closed wrapper themselves, in the right direction-specific shape — a requirement that leaks library-internal types into consumer code and breaks whenever a wrapper is added.

Take the other route: never ask `System.Text.Json` to resolve a contract for a library-owned type. Every one of those types is already serialized by a library-owned converter, so the library can supply the contract itself, and the consumer's context then only ever needs their own value type. Both mechanisms are verified against a consumer context declaring `string` alone, with reflection disabled.

**Write.** Replace `JsonSerializer.Serialize(writer, envelope, options.SerializerOptions)` in `CloudEventsResultExtensions` with a direct call to the existing public `writer.WriteCloudEvents(envelope, options)`. This is not new machinery: the converter that the current call dispatches into already delegates to that extension, so the `JsonSerializer` hop is a detour through contract resolution for a type the library fully controls. Removing it also removes the warnings at those sites. Metadata is unaffected — it is written through the manual `Utf8JsonWriter` extensions and needs no contract.

**Read.** Build the wrapper's contract from the library's own converter and use the `JsonTypeInfo` overloads:

```csharp
var wrapperInfo = JsonMetadataServices.CreateValueInfo<HttpReadAutoSuccessResultPayload<T>>(
    options,
    new HttpReadAutoSuccessResultPayloadJsonConverter<T>()
);
var payload = await JsonSerializer.DeserializeAsync(contentStream, wrapperInfo).ConfigureAwait(false);
```

`CreateValueInfo` is the source generator's own building block, involves no reflection, and is analyzer-clean: a project compiled with `IsAotCompatible=true` reports zero IL warnings for the write and read shapes above. The same treatment applies to the non-generic payloads and to the CloudEvents read path.

Two constraints on the implementation. `CreateValueInfo` allocates a `JsonTypeInfo` per call, so cache per `JsonSerializerOptions` instance — a static per-closed-type cache keyed by options, not a new contract on every serialization; this library does not allocate on hot paths. Creation against already-read-only options is safe, so the cache may be lazy: verified after an explicit `MakeReadOnly()`, which is the state a long-lived `Default` instance reaches after first use.

`CloudEventsEnvelopeForWritingJsonConverterFactory` and `HttpReadSuccessResultPayloadJsonConverterFactory` stay public and unchanged, with their existing `MakeGenericType` suppressions, for consumers who serialize the wrappers themselves through reflection-backed options. They simply leave the library's own path.

This also improves the defaults. `PortableResultsCloudEventsWriteOptions.Default` and `PortableResultsHttpReadOptions.Default` stay reflection-backed for the consumer's `T` — no library can supply that contract, and putting a source-generated resolver on the shared defaults would silently stop arbitrary `T` from serializing for the non-AOT majority. But once the library owns its own contracts, the defaults stop failing on library-owned types, so a non-generic `Result` should need no consumer registration at all. Confirm that during implementation and state it in the README; it is the difference between "AOT needs setup" and "AOT needs setup proportional to your own payload types".

### Proving it, in tests and in CI

The in-process tests are the primary proof for this issue; the ILC publish is regression protection for the paths the sample already walks.

In-process tests compose options whose only resolver is a test-local source-generated context declaring the value type and nothing else, and assert that a non-generic `Result` and a `Result<T>` round-trip over CloudEvents and HTTP. That the context declares no library-owned type is itself part of the assertion. A path that completes with no reflection-based resolver in the graph cannot have needed runtime code generation, which is the property under test. Do not toggle `System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault` from a test: the value is read once and cached per process, and the test hosts are shared and parallel. The switch remains the right manual reproduction, via a standalone console app with `<JsonSerializerIsReflectionEnabledByDefault>false</JsonSerializerIsReflectionEnabledByDefault>`, and that is how the failures in the Rationale were confirmed.

The ILC publish covers `samples/NativeAotMovieRating`, which exercises the Minimal API write path exclusively — the path that already works. It therefore does not verify the CloudEvents write or HTTP read paths in this release; extending the sample to walk them is deferred to a follow-up the maintainer will drive. Keep the gate anyway: it proves ILC still succeeds after the `IsAotCompatible` and `JsonTypeInfo` changes, and it is the seam the later sample work plugs into.

For the gate to report anything at all from this library, one sample setting has to change. The sample sets `SuppressTrimAnalysisWarnings=true` to silence Serilog's unannotated assembly rollup, which equally hides every warning originating in Light.PortableResults. Replace it with a targeted suppression scoped to the Serilog assemblies, and set `TrimmerSingleWarn=false` so individual warnings are reported rather than collapsed into one per assembly. This is the only change the sample needs here, and it adds no new code paths.

Add the publish to `build-and-test.yml` as a separate step after the existing test steps, on the runner's own RID. It is the slowest step in the workflow; keep it out of the matrix and do not gate the coverage jobs on it.

### Deliberately out of scope

- **`Light.PortableResults.AspNetCore.Mvc`.** MVC is not Native AOT compatible, and its package description does not claim otherwise.
- **Putting a source-generated resolver on the shared `Default` options.** Rejected above: only the consumer's context can supply a contract for their `T`, and a source-generated resolver on the shared defaults would break arbitrary `T` for the non-AOT majority.
- **Shipping `JsonSerializerContext` types for library-owned payloads.** Superseded — with the library building its own contracts from its own converters, there is nothing left for a shipped context to declare. Add one only if triage finds a site where converter-backed contract creation is impractical, and record why.
- **Extending the Native AOT sample to exercise the CloudEvents write and HTTP read paths.** Deferred to a follow-up the maintainer will drive. Until then the ILC gate covers the Minimal API write path only, and the in-process resolver-composition tests carry the proof for the paths this issue fixes.
- **Trim-only (`PublishTrimmed`) verification as a separate gate.** The AOT publish subsumes the trim analyzer for this code; a dedicated trimmed-but-not-AOT configuration would add a second slow CI step for no additional signal.
- **The `default(Result<T>)` write guard** and **`EnablePackageValidation`.** Tracked separately as items 2 and 3 of #77.
