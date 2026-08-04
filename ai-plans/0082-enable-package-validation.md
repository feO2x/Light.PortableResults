# Enable Package Validation Across All Packages

## Rationale

`EnablePackageValidation` is set only on `Light.PortableResults`, and no `PackageValidationBaselineVersion` is configured anywhere. Package validation hangs off the `Pack` target, and `build-and-test.yml` never packs — the only workflow that does is `release-on-nuget.yml`, which fires on `release: published`. A shape break would therefore surface in the publish job of an already-tagged release, never during review.

The gate should also run locally with no arguments. A check that exists only in CI is a check that AI coding agents and developers discover after pushing; one that a bare `dotnet pack` reproduces is a feedback loop they can close before committing. That constraint shapes the design below more than the CI job does.

Without a baseline the property is close to decorative: it activates only the compatible-framework check, which has teeth for the two multi-targeted packages and is a no-op for the other five. The baseline check is the part that earns its keep, and 0.7.0 is the release that most needs it — the core package's release notes carry roughly twenty-five breaking-change bullets, and nothing today distinguishes the ones that break a 0.6.0 consumer from the ones that only break someone tracking `main`. Measured against a 0.6.0 baseline, the entire solution produces exactly two consumer-visible breaks. Turning the baseline on before the release converts that distinction from prose into a reviewed, enforced artifact; turning it on afterwards means the release with the largest breaking-change surface is the one release that never gets audited.

## Acceptance Criteria

- [ ] Every packable project under `src/` builds with package validation enabled. The non-packable source generator is unaffected.
- [ ] All seven published packages validate against a `0.6.0` baseline, and an undeclared shape break fails the build.
- [ ] `dotnet pack -c Release` reproduces the CI gate exactly, with no additional arguments, properties or scripts. The inner `dotnet build` and `dotnet test` loop is unaffected.
- [ ] Package validation runs on push and pull request, so a break is caught in review rather than during the release job.
- [ ] Validation compares like with like on strong naming: a build without the release SNK produces no `CP0003`, and the release job keeps producing genuinely signed assemblies. Packages built for validation are never pushed.
- [ ] Strong-naming the `src` assemblies by default leaves the test suite and the Native AOT sample publish green.
- [ ] `AGENTS.md` documents the local command, its expected clean output, and the offline escape hatch.
- [ ] The only committed suppressions are the two `MetadataKind` enum-value changes already named in the release notes. Every other break listed there is confirmed to be either behavioral or invisible to a 0.6.0 consumer.
- [ ] The gate is proven with an injected regression: removing or renaming a public member fails the build with the corresponding `CP` diagnostic.
- [ ] The post-release step — bump the baseline to `0.7.0` and delete the suppression file — is recorded where it will be found at release time.

## Technical Details

### What the baseline does and does not catch

ApiCompat compares assembly shape. Most of the 0.7.0 breaking-change bullets are behavioral — decimal serializing as a JSON number, header quoting, null CloudEvents attributes being omitted, the new default-instance guard throwing — and none of them will ever produce a diagnostic. The prose release notes stay load-bearing; validation makes only the shape subset enforceable.

A large share of the remaining bullets describe breaks against intermediate states on `main` rather than against 0.6.0. `CanonicalFloatingPointFormatter`, `PortableResultsJsonContracts`, `MustNotBeDefaultInstance` and `HttpHeaderValueFormatter` were never in the published 0.6.0 assembly, so their removal or renaming cannot break a consumer. This is why the measured break count is two rather than twenty-five, and it is worth stating in the release notes review rather than treating the low number as a tooling failure.

The measured result against `0.6.0`, across every package and both assets, is the complete suppression file at `src/Light.PortableResults/CompatibilitySuppressions.xml`:

```xml
<!-- Exact generated content, abbreviated to the discriminating elements. -->
<Suppression>
  <DiagnosticId>CP0011</DiagnosticId>
  <Target>F:Light.PortableResults.Metadata.MetadataKind.Array</Target>
  <Left>lib/netstandard2.0/Light.PortableResults.dll</Left>
  <Right>lib/netstandard2.0/Light.PortableResults.dll</Right>
  <IsBaselineSuppression>true</IsBaselineSuppression>
</Suppression>
<!-- plus the identical entry for MetadataKind.Object -->
```

Both are the deliberate renumbering to 200 and 201 that reserves 16–199 for future primitive kinds. Only `netstandard2.0` entries are required; the `net10.0` asset needs none. Generate the file with `/p:ApiCompatGenerateSuppressionFile=true` rather than hand-writing it, and treat any third entry appearing during implementation as a finding to escalate, not to suppress.

Unnecessary suppressions fail the build by default, which is load-bearing for the handover below: once the baseline moves to `0.7.0`, these two entries become stale and the build fails until they are deleted. The cleanup cannot be forgotten silently, so do not set `ApiCompatPermitUnnecessarySuppressions`.

Confirm the gate is real before trusting a clean run. A `0.4.0` baseline for `Light.PortableResults.AspNetCore.MinimalApis` correctly reports `CP0001` for `PortableResultsEndpointExtensions`, the type extracted into the OpenApi package in 0.5.0; that check, or an equivalent injected regression, satisfies the proof criterion.

All seven packages published a `0.6.0`, so a single `PackageValidationBaselineVersion` in `src/Directory.Build.props` covers them. `AspNetCore.OpenApi` and `Validation.OpenApi` first shipped in 0.5.0, which does not affect a 0.6.0 baseline. The baseline `PackageDownload` does not enter `packages.lock.json` and does not trip `RestoreLockedMode`, so no lock file changes are expected — a lock file diff during implementation means something else moved.

### Strong naming

This is the blocker. Published packages are strong-named through an SNK that exists only as a GitHub Actions secret, so ordinary builds are unsigned and every package fails the baseline on identity alone:

```
CP0003: [Baseline] ... assembly public key token 'cc46d8340219f3bd' does not match ... 'null'
```

`Light.PortableResults.Public.snk` is already committed at the repository root and carries the matching token. Do not suppress `CP0003` instead — that would hide a real signing regression at the one point where it is observable.

Passing the signing properties on the command line would work, but it puts the gate behind an invocation nobody remembers, which defeats the local-loop goal. Default them in `src/Directory.Build.props` instead, keyed off whether a key file was supplied:

```xml
<!-- Exact. Order-independent: the guard reads AssemblyOriginatorKeyFile, it does not set it. -->
<UsePublicSigningKey Condition="'$(AssemblyOriginatorKeyFile)' == ''">true</UsePublicSigningKey>
<SignAssembly Condition="'$(UsePublicSigningKey)' == 'true'">true</SignAssembly>
<PublicSign Condition="'$(UsePublicSigningKey)' == 'true'">true</PublicSign>
<AssemblyOriginatorKeyFile Condition="'$(UsePublicSigningKey)' == 'true'">$(MSBuildThisFileDirectory)../Light.PortableResults.Public.snk</AssemblyOriginatorKeyFile>
```

The release workflow passes `AssemblyOriginatorKeyFile` as a global property, which cannot be overridden from a props file, so `UsePublicSigningKey` stays empty there and `PublicSign` is never set — the release build signs with the private key exactly as it does today. Verify this with `-getProperty:PublicSign` under both invocations rather than by inspection; it is the one place where a mistake ships unsigned-but-strong-named packages to NuGet. Do not route the guard through `SignAssembly`: the release workflow sets that too, so it cannot distinguish the two cases.

Use `$(MSBuildThisFileDirectory)` rather than the `../../` that `release-on-nuget.yml` uses. The relative form resolves against the project file and only works because every packable project sits exactly two levels down.

The consequence is that all `src` assemblies become strong-named in every build, not just when packing. That is a real change and the reason the acceptance criteria call for the test suite and the AOT sample: public-signed assemblies carry the strong name without a valid signature, which .NET Core does not verify but which would break `InternalsVisibleTo` if the solution used it — it does not. It is also what lets CI validate with a `--no-build` pack over the existing build output, as described below. Packages produced outside the release workflow must never be pushed.

### CI shape and the local loop

Package validation hangs off `Pack`, so `dotnet build` and `dotnet test` stay untouched and the inner loop keeps its current speed. Agents and developers opt in with:

```shell
dotnet pack ./Light.PortableResults.slnx -c Release
```

No properties, no script, no separate target — the same command CI runs, and on a warm NuGet cache it completes in a few seconds. Document it in `AGENTS.md` next to the existing build and test guidance, including that a clean run prints only `Successfully created package` lines, and that a break prints a `CP` diagnostic naming the API.

Setting a baseline adds a `PackageDownload` for the seven 0.6.0 packages, so a cold restore needs network access; the CI NuGet cache already covers this. `-p:DisablePackageBaselineValidation=true` is the escape hatch for working offline, and it belongs in the `AGENTS.md` note so it is not rediscovered as a workaround for a genuine break.

In `build-and-test.yml` this is a step, not a job. Because signing is defaulted in the props file, the existing build job already produces the strong-named assemblies validation needs, so a `--no-build` pack reuses them and adds a couple of seconds:

```shell
dotnet pack ./Light.PortableResults.slnx --configuration Release --no-build /p:ContinuousIntegrationBuild=true
```

Verified: `--no-build` still runs `RunPackageValidation`. Nothing here warrants the separate-job treatment `native-aot-publish` gets — that split exists because native compilation is slow, which does not apply. Put the step after the test run so a validation failure does not mask a test failure. The packages it produces are a byproduct and must not be pushed.

### Release handover

After 0.7.0 is tagged, `PackageValidationBaselineVersion` moves to `0.7.0` and `src/Light.PortableResults/CompatibilitySuppressions.xml` is deleted in the same commit, so the 0.8.0 cycle starts from a clean baseline with no suppressions. The repository has no release checklist document today, so record this as a follow-up issue on #77 rather than inventing one as part of this change.

### Deliberately out of scope

- Package validation for `Light.PortableResults.Validation.OpenApi.SourceGeneration`. It sets `IsPackable=false`, so validation never runs for it and nothing needs to opt it out.
- Runtime-specific asset validation. No package ships RID-specific assets.
- Reverting or reworking the `MetadataKind` renumbering. It is intentional and already documented; the suppression records it.
- Any change to the release signing model. Ordinary builds do gain public signing, but the private key, the workflow and the published artifacts are untouched.
- A dedicated MSBuild target, script or `dotnet` tool wrapping the local check. `dotnet pack -c Release` is the whole interface.
- Making behavioral breaks enforceable. ApiCompat cannot see them and the release notes remain the record.
- The remaining v0.7.0 preparations. This is item 3 of #77; Native AOT compatibility is #78 and the default-result write guard is #80.
