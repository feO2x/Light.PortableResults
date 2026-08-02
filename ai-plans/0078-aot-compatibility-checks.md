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
- [ ] When the consumer's value type is unresolvable, every affected entry point throws an exception that names the unresolved type and states the remedy, and a negative test pins that behavior per site. The unreachable guards in `SystemTextJsonWritingExtensions`, `LightResult`, and `LightActionResult` are converted so they can fire.
- [ ] Behavior for consumers who pass reflection-backed options is unchanged: the existing test suites pass without modification beyond additions, and `PortableResultsCloudEventsWriteOptions.Default` and `PortableResultsHttpReadOptions.Default` keep serializing arbitrary `T` as they do today.
- [ ] Converters supplied through `JsonSerializerOptions` still take precedence over the library's own, including replacements inserted ahead of the defaults. The library-created contract is reached only when the configured resolver cannot supply one.
- [ ] CI publishes the Native AOT sample and fails on any trim or AOT diagnostic originating in a Light.PortableResults assembly, while the sample's unannotated package dependencies stay non-fatal. The sample's blanket `SuppressTrimAnalysisWarnings` is gone, and the replacement is demonstrated to fail the publish on an injected library-side regression.
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

Every warning is a call to a `JsonSerializer.Serialize`/`Deserialize<T>(…, JsonSerializerOptions)` overload. The payload type is statically known at each site; only the resolver is the caller's. The fix is to resolve a `JsonTypeInfo<T>` from the options and call the `JsonTypeInfo` overload, which is analyzer-clean. `SystemTextJsonWritingExtensions`, `LightResult`, and `LightActionResult` already do this, but resolve through `options.GetTypeInfo(typeof(T))` — see the next section before copying them.

This is the decision that shapes the rest of the work, so record why the alternative was rejected. Annotating the public entry points with `[RequiresDynamicCode]`/`[RequiresUnreferencedCode]` would be permanently wrong: these methods do not intrinsically require dynamic code, they require the caller's options to carry a resolver. Annotating would force a suppression on every correctly configured AOT consumer while doing nothing for an incorrectly configured one. Blanket `UnconditionalSuppressMessage` at the call sites is equally wrong here — it silences a diagnostic that is currently accurate. The four existing factory suppressions stay untouched: `MakeGenericType` over a resolved generic is genuinely safe when the closed type is registered, which is what their justifications already state.

### Resolve with `TryGetTypeInfo`, and repair the existing guards

`JsonSerializerOptions.GetTypeInfo` does not return `null` for an unknown type — it throws `NotSupportedException` first. Verified against a source-generated resolver declaring only `string`:

```
GetTypeInfo(typeof(HttpResultForWriting))
  -> NotSupportedException: JsonTypeInfo metadata for type '…HttpResultForWriting' was not provided by
     TypeInfoResolver of type 'OnlyStringContext'. …
TryGetTypeInfo(typeof(HttpResultForWriting), out var info)
  -> false, info is null
```

So a `GetTypeInfo` call followed by a null check or a failed-cast check can never produce the intended exception; the guard is unreachable for the missing-type case, which is the only case that occurs in practice. Three existing guards are dead code for this reason and must be converted as part of this work, not copied: `SystemTextJsonWritingExtensions` (the `is null` check after `GetTypeInfo`; the `TryGetTypeInfo` call later in the same method is already correct and is the model), `LightResult`, and `LightActionResult`.

Resolve through `options.TryGetTypeInfo(typeof(T), out var typeInfo)` and translate the negative result into an `InvalidOperationException` that names the unresolved type and states the remedy. Keep `InvalidOperationException` for consistency with the existing wording in `LightResult`, and keep the message specific to what the caller must do — register their value type with the context supplied to these options. `System.Text.Json`'s own message names the type and the resolver but cannot mention the library-specific setup, and the "reflection has been disabled" variant names no type at all.

Every converted site needs a negative test asserting the exception type and that the message names the unresolved type; without it the guards stay untested, which is how the current three came to be unreachable.

### Library-owned contracts, not resolver composition

Resolver composition cannot carry this. `JsonTypeInfoResolver.Combine` queries each resolver in order and returns the first contract that matches the requested type; it never synthesizes `CloudEventsEnvelopeForWriting<MyDto>` from a contract for the envelope and another for `MyDto`. Verified: with a consumer context declaring only `string`, the current write path throws `NotSupportedException` for `CloudEventsEnvelopeForWriting<string>`. Shipping library contexts and combining them would therefore not spare consumers from declaring every closed wrapper themselves, in the right direction-specific shape — a requirement that leaks library-internal types into consumer code and breaks whenever a wrapper is added.

Take the other route: when `System.Text.Json` cannot resolve a contract for a library-owned type, let the library supply one. Every such type is already serialized by a library-owned converter, so the consumer's context only ever needs their own value type. Verified against a consumer context declaring `string` alone, with reflection disabled: a direct `writer.WriteCloudEvents(envelope, options.SerializerOptions)` writes a full CloudEvent including metadata, and a contract built with `JsonMetadataServices.CreateValueInfo<HttpReadAutoSuccessResultPayload<T>>(options, new HttpReadAutoSuccessResultPayloadJsonConverter<T>())` deserializes correctly. `CreateValueInfo` is the source generator's own building block, involves no reflection, and both shapes are analyzer-clean under `IsAotCompatible=true`.

**The library-created contract must be the fallback, never the first choice.** Hardcoding the library converter would ignore converters supplied through `JsonSerializerOptions`, and existing tests depend on exactly that: `HttpResponseMessageExtensionsTests` inserts `NullBareStringPayloadConverter` and `EmptyFailurePayloadConverter` at index 0 and asserts the read path uses them. Resolve in this order at every converted site:

1. If `options.TypeInfoResolver is null` and `JsonSerializer.IsReflectionEnabledByDefault`, call `options.MakeReadOnly(populateMissingResolver: true)`.
2. If `options.TryGetTypeInfo(typeof(TWrapper), out var info)` and `info is JsonTypeInfo<TWrapper> typed`, serialize with `typed`.
3. Otherwise use the cached library-created contract, or the direct writer on the CloudEvents write path.

Step 1 is not optional, and it is the step that is easy to miss. The library's own default options never assign a `TypeInfoResolver` — `CreateDefaultSerializerOptions` only adds converters — so `TryGetTypeInfo` returns `false` on them until a resolver is materialized, and without step 1 every reflection-backed caller would silently route to the fallback and lose their replacement converters. Verified: on default HTTP read options with a replacement converter at index 0, `TryGetTypeInfo` returns `false` beforehand and `true` afterwards, and the replacement converter is the one that runs.

`MakeReadOnly(bool)` carries IL2026 and IL3050 because it may construct the reflection resolver. Guarding the call with `JsonSerializer.IsReflectionEnabledByDefault` makes it unreachable when reflection is off, which is precisely what that property exists for, so suppress both codes there with that justification. This is the one place in the new design where a suppression is correct. Freezing the options is not a behavior change: the paths that reach step 1 are about to serialize with them, which freezes them anyway.

Under AOT the sequence degrades exactly as intended: step 1 is skipped, step 2 fails for a consumer context declaring only `T`, and step 3 carries the call. Under reflection, step 2 always wins and behavior is unchanged.

Two constraints on step 3. `CreateValueInfo` allocates a `JsonTypeInfo` per call, so cache per `JsonSerializerOptions` instance — a static per-closed-type cache keyed by options, not a new contract on every serialization; this library does not allocate on hot paths. Creation against already-read-only options is safe, so the cache may be lazy: verified after an explicit `MakeReadOnly()`, which is the state a long-lived `Default` instance reaches after first use. And even in the fallback, prefer a converter registered on `options.Converters` for the wrapper type over the library default, so a consumer who replaces a converter is honored under AOT too.

Four factories carry `MakeGenericType` suppressions and all stay public and unchanged: `CloudEventsEnvelopeForWritingJsonConverterFactory`, `CloudEventsSuccessPayloadJsonConverterFactory`, and `HttpReadSuccessResultPayloadJsonConverterFactory` sit on the three paths this work touches and continue to serve step 2, while `HttpResultForWritingJsonConverterFactory` is on the HTTP write path and is not involved at all.

This also improves the defaults. `PortableResultsCloudEventsWriteOptions.Default` and `PortableResultsHttpReadOptions.Default` stay reflection-backed for the consumer's `T` — no library can supply that contract, and putting a source-generated resolver on the shared defaults would silently stop arbitrary `T` from serializing for the non-AOT majority. But once the library owns its own contracts, the defaults stop failing on library-owned types, so a non-generic `Result` should need no consumer registration at all. Confirm that during implementation and state it in the README; it is the difference between "AOT needs setup" and "AOT needs setup proportional to your own payload types".

### Proving it, in tests and in CI

The in-process tests are the primary proof for this issue; the ILC publish is regression protection for the paths the sample already walks.

In-process tests compose options whose only resolver is a test-local source-generated context declaring the value type and nothing else, and assert that a non-generic `Result` and a `Result<T>` round-trip over CloudEvents and HTTP. That the context declares no library-owned type is itself part of the assertion. A path that completes with no reflection-based resolver in the graph cannot have needed runtime code generation, which is the property under test. Do not toggle `System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault` from a test: the value is read once and cached per process, and the test hosts are shared and parallel. The switch remains the right manual reproduction, via a standalone console app with `<JsonSerializerIsReflectionEnabledByDefault>false</JsonSerializerIsReflectionEnabledByDefault>`, and that is how the failures in the Rationale were confirmed.

The ILC publish covers `samples/NativeAotMovieRating`, which exercises the Minimal API write path exclusively — the path that already works. It therefore does not verify the CloudEvents write or HTTP read paths in this release; extending the sample to walk them is deferred to a follow-up the maintainer will drive. Keep the gate anyway: it proves ILC still succeeds after the `IsAotCompatible` and `JsonTypeInfo` changes, and it is the seam the later sample work plugs into.

For the gate to report anything at all from this library, one sample setting has to change. The sample sets `SuppressTrimAnalysisWarnings=true` to silence Serilog's unannotated assembly rollup, which equally hides every warning originating in Light.PortableResults. Drop it, and keep single-warning mode rather than turning it off:

```xml
<!-- Serilog is not yet fully annotated for trimming. Keep the per-assembly rollup for package
     references and demote only that rollup, so project references keep reporting in full. -->
<WarningsNotAsErrors>$(WarningsNotAsErrors);IL2104</WarningsNotAsErrors>
```

Do not set `TrimmerSingleWarn=false`. Verified on this sample: it expands Serilog into individual diagnostics, `Serilog.Capturing.PropertyValueConverter.TryConvertStructure` raises `IL2067`, the repository's Release `TreatWarningsAsErrors` promotes it to an error, and ILC fails with `MSB3077`. Suppressing that through `NoWarn` is not an option either — `IL2067` is not Serilog-specific, and silencing it globally would hide the same diagnostic in Light assemblies.

The configuration above is not a blunt suppression, because the SDK applies single-warn to package references while project references always report in detail. Verified end to end by injecting a `JsonSerializer.Serialize(value, value.GetType())` call into `Light.PortableResults` and publishing the sample:

| Origin | Reported as | Fatal |
| --- | --- | --- |
| `Serilog.dll` (package reference) | one `IL2104` rollup | no |
| `Light.PortableResults` (project reference) | `IL3050` and `IL2026`, with file and line | yes |

The injected regression failed the publish while Serilog stayed quiet, which is exactly the asymmetry the gate needs. Note this also holds today, before the analyzers are enabled: `IsAotCompatible` governs compile-time analysis, so ILC is the only thing that would have caught such a call in the current codebase.

Add the publish to `build-and-test.yml` as a separate step after the existing test steps, on the runner's own RID. Publish the sample project directly and do not pass `/p:PublishAot=true` on the command line: as a global property it flows into every project reference and fails the `netstandard2.0` source generator with `NETSDK1207`. The sample already sets `PublishAot` internally and strips it from its references via `GlobalPropertiesToRemove`, which is why `dotnet publish <sample> -c Release -r <rid>` is the correct invocation. It is the slowest step in the workflow; keep it out of the matrix and do not gate the coverage jobs on it.

### Deliberately out of scope

- **Native AOT support for `Light.PortableResults.AspNetCore.Mvc`.** MVC is not Native AOT compatible, and its package description does not claim otherwise, so it gets no `IsAotCompatible` and no analyzer triage. The unreachable guard in `LightActionResult` is still repaired: it is the same defect as the other two and is not an AOT concern.
- **Putting a source-generated resolver on the shared `Default` options.** Rejected above: only the consumer's context can supply a contract for their `T`, and a source-generated resolver on the shared defaults would break arbitrary `T` for the non-AOT majority.
- **Shipping `JsonSerializerContext` types for library-owned payloads.** Superseded — with the library building its own contracts from its own converters, there is nothing left for a shipped context to declare. Add one only if triage finds a site where converter-backed contract creation is impractical, and record why.
- **Extending the Native AOT sample to exercise the CloudEvents write and HTTP read paths.** Deferred to a follow-up the maintainer will drive. Until then the ILC gate covers the Minimal API write path only, and the in-process resolver-composition tests carry the proof for the paths this issue fixes.
- **Trim-only (`PublishTrimmed`) verification as a separate gate.** The AOT publish subsumes the trim analyzer for this code; a dedicated trimmed-but-not-AOT configuration would add a second slow CI step for no additional signal.
- **The `default(Result<T>)` write guard** and **`EnablePackageValidation`.** Tracked separately as items 2 and 3 of #77.
