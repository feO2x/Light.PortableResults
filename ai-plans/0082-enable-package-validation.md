# Enable Package Validation Across All Packages

> Correction: the suggested probe value in Technical Details does not compile. Setting `MetadataKind.String` to `15` collides with an existing kind and yields `CS0152` and `CS8510` instead of a `CP` diagnostic — exactly the masked-failure mode the following sentence warns about, reached through a value collision rather than through `CS1591`. Pick a value inside the reserved 16–199 range; `150` was used to verify the gate. The rest of the probe reasoning holds: the 0.6.0 enum does contain `String`, so changing its value reports `CP0011` on the `netstandard2.0` asset.

## Rationale

Only `Light.PortableResults` enables package validation, no package has a baseline, and validation currently runs only when `release-on-nuget.yml` packs an already-tagged release. An API shape break therefore reaches review only as prose and reaches automation too late.

Enable validation for every published package against `0.6.0`, run it on push and pull request, and keep a bare `dotnet pack` as the exact local gate. A check that lives only in CI is one that agents and developers discover after pushing, so the local loop is a design constraint here, not a convenience. Without a baseline, only the compatible-framework check runs—useful for the two multi-targeted packages and a no-op for the other five. This matters particularly for 0.7.0: roughly twenty-five release-note bullets describe breaks, but ApiCompat shows that only two affect a 0.6.0 consumer. Baseline now rather than after the release, or the version carrying the largest breaking-change surface becomes the only one never audited.

## Acceptance Criteria

- [x] All seven packable projects under `src/` validate against `0.6.0`; the non-packable source generator does not run package validation, and an undeclared shape break fails the build.
- [x] `dotnet pack -c Release` reproduces the CI gate without extra arguments, properties or scripts. Validation does not run during build or test; only restore gains the seven baseline packages.
- [x] One documented property disables baseline acquisition and comparison so restore, build, test and pack work offline. Compatible-framework validation remains active, lock files remain unchanged, and locked restore succeeds.
- [x] Validation runs on push and pull request. The CI NuGet cache includes baseline inputs and rotates when the baseline version changes; validation packages are never pushed.
- [x] Ordinary builds use the matching public signing key and produce no `CP0003`; releases remain genuinely private-signed. Strong-naming all `src` assemblies leaves the full test suite and Native AOT sample publish green.
- [x] Solution packing emits no non-packable-project warning locally or during release.
- [x] `AGENTS.md` documents the local command, exit-code/diagnostic interpretation, restore impact, and offline escape hatch.
- [x] The only suppressions are the two intentional `MetadataKind` value changes already in the release notes; every other listed break is confirmed behavioral or absent from the 0.6.0 API.
- [x] An injected public-member regression fails clean and incremental packs with the corresponding `CP` diagnostic and a non-zero exit code.
- [x] The release-time handover to a `0.7.0` baseline with no suppression file is recorded where it will be found.

## Technical Details

### Baseline and suppressions

ApiCompat compares assembly shape, not behavior. Changes such as JSON number encoding, header quoting, omitted null CloudEvents attributes and the default-result guard remain enforced only by tests and release notes. Other apparent breaks concern APIs that never shipped in 0.6.0, including `CanonicalFloatingPointFormatter`, `PortableResultsJsonContracts`, `MustNotBeDefaultInstance` and `HttpHeaderValueFormatter`.

All seven packages published 0.6.0; `AspNetCore.OpenApi` and `Validation.OpenApi` first shipping in 0.5.0 does not affect that baseline. A single `PackageValidationBaselineVersion` in `src/Directory.Build.props` therefore covers every packable source project.

The complete measured break set is `CP0011` for `MetadataKind.Array` and `MetadataKind.Object`, whose values intentionally moved from 5/6 to 200/201 to reserve 16–199 for future primitive kinds. Only the `netstandard2.0` asset needs suppressions; `net10.0` has no baseline counterpart. Generate `src/Light.PortableResults/CompatibilitySuppressions.xml` with `/p:ApiCompatGenerateSuppressionFile=true`; its two discriminating entries are:

```xml
<Suppression>
  <DiagnosticId>CP0011</DiagnosticId>
  <Target>F:Light.PortableResults.Metadata.MetadataKind.Array</Target>
  <Left>lib/netstandard2.0/Light.PortableResults.dll</Left>
  <Right>lib/netstandard2.0/Light.PortableResults.dll</Right>
  <IsBaselineSuppression>true</IsBaselineSuppression>
</Suppression>
<!-- Identical entry for MetadataKind.Object. -->
```

Treat any third generated entry as a finding, not another suppression. Unnecessary suppressions must fail ordinary builds so the post-release baseline bump forces deletion of this file; permit them only for the offline escape hatch described below.

Prove the gate with an injected regression. Changing `MetadataKind.String` from 4 is suitable because the 0.6.0 enum contains only `Null`, `Boolean`, `Int64`, `Double`, `String`, `Array` and `Object`. An alternative is a 0.4.0 baseline for `Light.PortableResults.AspNetCore.MinimalApis`, which reports `CP0001` for the later-extracted `PortableResultsEndpointExtensions`. Verify both a clean pack and an already-packed incremental tree. Give any injected public API XML doc comments: `TreatWarningsAsErrors` turns `CS1591` into a Release build error, and a grep for `CP` diagnostics hides that failure, so the probe reads as validation being broken.

### Strong naming

The 0.6.0 packages are strong-named, while ordinary builds are unsigned, causing `CP0003` for token `cc46d8340219f3bd` versus `null`. `Light.PortableResults.Public.snk` contains the matching public key. Do not suppress this diagnostic; default ordinary `src` builds to public signing instead. Default it in the props file rather than passing the properties on the command line: a gate reachable only through an invocation nobody remembers is not a local gate.

```xml
<UsePublicSigningKey Condition="'$(AssemblyOriginatorKeyFile)' == ''">true</UsePublicSigningKey>
<SignAssembly Condition="'$(UsePublicSigningKey)' == 'true'">true</SignAssembly>
<PublicSign Condition="'$(UsePublicSigningKey)' == 'true'">true</PublicSign>
<AssemblyOriginatorKeyFile Condition="'$(UsePublicSigningKey)' == 'true'">$(MSBuildThisFileDirectory)../Light.PortableResults.Public.snk</AssemblyOriginatorKeyFile>
```

The guard reads rather than sets `AssemblyOriginatorKeyFile`, so snippet order is irrelevant. The release workflow supplies that property globally together with `SignAssembly`; the props file cannot override it, `UsePublicSigningKey` stays empty, and `PublicSign` remains unset. Verify `PublicSign` with `-getProperty` for both invocations rather than by inspection: this is the one place where a mistake ships strong-named but unsigned packages to NuGet. Do not guard on `SignAssembly`, which the release also sets.

Use `$(MSBuildThisFileDirectory)` for the committed key. The release workflow's `../../` resolves relative to each project and works only because all packable projects are two levels below the root.

All `src` assemblies consequently become strong-named in ordinary builds. Public signing carries the strong-name identity without a valid private signature; .NET Core accepts it, but it would affect `InternalsVisibleTo`, which this solution does not use. Run the full suite and Native AOT publish, and never push packages produced outside the private-signing release workflow.

### Local, offline and CI execution

The local gate is:

```shell
dotnet pack ./Light.PortableResults.slnx -c Release
```

Judge success by exit code and absence of `CP` diagnostics, not console text: incremental runs may print package lines, build lines, or nothing. Confirm an injected break returns non-zero and names the API. `--no-build` still runs `RunPackageValidation` over previously built outputs.

Solution packing currently warns because the Native AOT web sample is non-packable and `Microsoft.NET.Sdk.Web.ProjectSystem.props` defaults `WarnOnPackingNonPackableProject` to `true`. Set it to `false` in the sample; other non-packable projects already remain quiet.

Baseline acquisition occurs during every restore, not only pack: `Microsoft.NET.ApiCompat.targets` adds an evaluation-time `PackageDownload`. Thus implicit restore in `dotnet build` or `dotnet test` pays one cold download of seven small packages, while compilation and test execution after restore are unchanged. Avoid conditioning on the undocumented `_IsPacking`; CI needs the baseline restored before its `--no-build` pack.

`PackageDownload` does not update `packages.lock.json`, so the existing cache key would not rotate and an immutable exact cache hit could never acquire the new packages. Include the baseline declaration in the key:

```yaml
key: nuget-${{ runner.os }}-${{ hashFiles('**/packages.lock.json', 'src/Directory.Build.props') }}
```

This rotates when the baseline changes without duplicating its version in the workflow; unrelated edits to the props file may rebuild the cache. The shared composite action covers both build and release workflows.

The offline escape hatch is `-p:DisablePackageBaselineValidation=true`, for example:

```shell
dotnet build ./Light.PortableResults.slnx -c Release -p:DisablePackageBaselineValidation=true
```

It suppresses the baseline download and comparison but leaves compatible-framework validation active. Because the two baseline suppressions then become unmatched and normally fail as unnecessary, scope the exception to this property:

```xml
<ApiCompatPermitUnnecessarySuppressions Condition="'$(DisablePackageBaselineValidation)' == 'true'">true</ApiCompatPermitUnnecessarySuppressions>
```

Verify with baseline packages absent that restore downloads none and build, tests and pack pass; verify ordinary pack still rejects stale suppressions. Locked restore under `ContinuousIntegrationBuild=true` must remain green with no lock-file diff or `NU1004`; the exact `PackageDownload` version preserves determinism.

Add this step to `build-and-test.yml` after tests so a validation failure does not mask a test failure:

```shell
dotnet pack ./Light.PortableResults.slnx --configuration Release --no-build /p:ContinuousIntegrationBuild=true
```

It reuses the public-signed Release outputs, still executes package validation, and produces unpushed package byproducts. A separate job is unnecessary; unlike Native AOT publication, validation is fast.

### Release handover

After 0.7.0 is tagged, change `PackageValidationBaselineVersion` to `0.7.0` and delete `src/Light.PortableResults/CompatibilitySuppressions.xml` in the same commit, starting the 0.8.0 cycle from a clean baseline. Record this as a follow-up issue on #77 because the repository has no release checklist.

### Deliberately out of scope

- Validation for `Light.PortableResults.Validation.OpenApi.SourceGeneration`, which is non-packable.
- RID-specific asset validation; no package ships RID-specific assets.
- Reworking the intentional `MetadataKind` renumbering or enforcing behavioral breaks through ApiCompat.
- Changing private release signing, making the sample packable, removing it from the solution, or wrapping pack in a custom target, script or tool.
- Other 0.7.0 preparation: this is item 3 of #77; Native AOT compatibility is #78 and the default-result write guard is #80.
