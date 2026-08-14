# Plan Deviations for Native AOT Compatibility Checks

## Referenced Plans

- `0078-0-aot-compatibility-checks.md` enabled `IsAotCompatible` on the `net10.0` assets of
  `Light.PortableResults` and `Light.PortableResults.Validation`, routed every warned serialization call through a
  `JsonTypeInfo<T>` overload, introduced library-owned contract resolution for the CloudEvents write, CloudEvents
  read, and HTTP read wrappers, and added an ILC publish gate to CI.

## Deviations

### A fourth unreachable guard in `ErrorsExtensions`

The plan listed `SystemTextJsonWritingExtensions`, `LightResult`, and `LightActionResult` as the sites whose
unreachable guards had to be converted. `ErrorsExtensions.WriteRichErrors` resolved `JsonTypeInfo<MetadataObject>`
through the same pattern — `GetTypeInfo` followed by a failed-cast guard that `NotSupportedException` reached
first — but the plan did not name it. It is converted with the same contract resolution.

It is the one converted site that supplies no library fallback converter: `GetLibraryTypeInfo<MetadataObject>` is
called without a `createLibraryConverter` argument, so only a converter configured on the options is used. The
correct `MetadataObject` converter differs per transport, because CloudEvents writing and HTTP writing register
their own, and the library cannot pick one without knowing the transport.

Without this change, a failure result carrying error metadata could not be written under a source-generated
resolver, and acceptance criterion 4 would not hold for failure results.

### `Light.PortableResults.Validation` was not clean

The plan recorded the package as "currently clean" and enabled its analyzers purely for regression protection.
Enabling `IsAotCompatible` instead produced `IL2091` on
`PortableResultsValidationModule.ValidateWithPortableResults<TOptions, TValidator>`. The framework annotates the
`TService` parameter of `ServiceCollectionDescriptorExtensions.TryAddSingleton<TService>` with
`DynamicallyAccessedMemberTypes.PublicConstructors`, and the method forwards its own open `TValidator` into it
without declaring the same requirement.

`TValidator` is therefore annotated with `DynamicallyAccessedMemberTypes.PublicConstructors`. The annotation is
load-bearing rather than cosmetic: the container constructs the validator reflectively, no IL calls its
constructor, and a trimmed constructor fails at runtime when the service is resolved.

This is a source-breaking change for callers that keep their own `TValidator` parameter open and forward it,
which is why it is recorded under `Breaking changes` in the package release notes. Applications passing a concrete
validator type are unaffected: the annotation is discharged at the call site, exactly as it already is for
`AddSingleton<TValidator>`. Enabling trim analyzers on a package believed to be clean is therefore not free — it
can cost public API surface.

### The publish gate is a separate job, not a post-test step

The plan asked for "a separate post-test step to `build-and-test.yml`" and, in the same section, that no coverage
job depend on it. Those two cannot both hold: `coverage-comment` declares `needs: build-and-test`, so any step
added to that job also gates the coverage comment behind the native compilation.

The publish runs as an independent `native-aot-publish` job instead. It preserves every constraint the plan gave
the step — runner RID, no `PublishAot` on the command line, outside any matrix — while satisfying the intent
behind the coverage constraint, and it now runs in parallel with the tests rather than after them.

### Contract resolution freezes the options on the fallback path

The plan called for freezing only in step 1, where `MakeReadOnly(populateMissingResolver: true)` materializes the
reflection resolver, and separately required library-owned contracts to be created once per `JsonSerializerOptions`
instance. Those two do not compose on the CloudEvents write path: invoking a converter directly never freezes the
options, so nothing marked them read-only and the cache was never populated.

Step 3 therefore calls `MakeReadOnly()` once the converter has been resolved and before the result is cached. The
observable consequence is that resolving a library-owned contract makes the options read-only, so converters
registered afterwards throw. This matches what the plan already accepted for step 1 — serializing with the
resolved contract would freeze the options immediately afterwards — and it is what keeps a later registration from
invalidating a cached contract.
