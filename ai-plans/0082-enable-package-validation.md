# Enable Package Validation Across All Packages

## Rationale

`EnablePackageValidation` is set only on `Light.PortableResults`, and no `PackageValidationBaselineVersion` is configured anywhere. Package validation hangs off the `Pack` target, and `build-and-test.yml` never packs — the only workflow that does is `release-on-nuget.yml`, which fires on `release: published`. A shape break would therefore surface in the publish job of an already-tagged release, never during review.

The gate should also run locally with no arguments. A check that exists only in CI is a check that AI coding agents and developers discover after pushing; one that a bare `dotnet pack` reproduces is a feedback loop they can close before committing. That constraint shapes the design below more than the CI job does.

Without a baseline the property is close to decorative: it activates only the compatible-framework check, which has teeth for the two multi-targeted packages and is a no-op for the other five. The baseline check is the part that earns its keep, and 0.7.0 is the release that most needs it — the core package's release notes carry roughly twenty-five breaking-change bullets, and nothing today distinguishes the ones that break a 0.6.0 consumer from the ones that only break someone tracking `main`. Measured against a 0.6.0 baseline, the entire solution produces exactly two consumer-visible breaks. Turning the baseline on before the release converts that distinction from prose into a reviewed, enforced artifact; turning it on afterwards means the release with the largest breaking-change surface is the one release that never gets audited.

## Acceptance Criteria

- [ ] Every packable project under `src/` builds with package validation enabled. The non-packable source generator is unaffected.
- [ ] All seven published packages validate against a `0.6.0` baseline, and an undeclared shape break fails the build.
- [ ] `dotnet pack -c Release` reproduces the CI gate exactly, with no additional arguments, properties or scripts. No validation runs during `dotnet build` or `dotnet test`, so compilation and test execution cost is unchanged after restore; restore itself gains the seven baseline packages, which a cold `dotnet build` or `dotnet test` pays for through implicit restore.
- [ ] A single documented property suppresses the baseline download at restore and baseline validation at pack, so the solution restores, builds, tests and packs offline. Compatible-framework validation still runs offline. `packages.lock.json` files stay unchanged and locked-mode restore keeps working.
- [ ] The CI NuGet cache key covers the baseline packages, so they are cached rather than re-downloaded on every run, and the key rotates when the baseline version changes.
- [ ] Package validation runs on push and pull request, so a break is caught in review rather than during the release job.
- [ ] Validation compares like with like on strong naming: a build without the release SNK produces no `CP0003`, and the release job keeps producing genuinely signed assemblies. Packages built for validation are never pushed.
- [ ] Strong-naming the `src` assemblies by default leaves the test suite and the Native AOT sample publish green.
- [ ] Packing the solution emits no warning for the non-packable sample, in the release workflow as well as locally.
- [ ] `AGENTS.md` documents the local command, how to tell success from failure, and the offline escape hatch.
- [ ] The only committed suppressions are the two `MetadataKind` enum-value changes already named in the release notes. Every other break listed there is confirmed to be either behavioral or invisible to a 0.6.0 consumer.
- [ ] The gate is proven with an injected regression: removing or renaming a public member fails with the corresponding `CP` diagnostic and a non-zero exit code, on an incremental pack as well as a clean one.
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

Unnecessary suppressions fail the build by default, which is load-bearing for the handover below: once the baseline moves to `0.7.0`, these two entries become stale and the build fails until they are deleted. The cleanup cannot be forgotten silently, so do not grant `ApiCompatPermitUnnecessarySuppressions` unconditionally. The one narrow exception, scoped to the offline escape hatch, is described below.

Confirm the gate is real before trusting a clean run. A `0.4.0` baseline for `Light.PortableResults.AspNetCore.MinimalApis` correctly reports `CP0001` for `PortableResultsEndpointExtensions`, the type extracted into the OpenApi package in 0.5.0; that check, or an equivalent injected regression, satisfies the proof criterion.

All seven packages published a `0.6.0`, so a single `PackageValidationBaselineVersion` in `src/Directory.Build.props` covers them. `AspNetCore.OpenApi` and `Validation.OpenApi` first shipped in 0.5.0, which does not affect a 0.6.0 baseline. Setting the baseline has restore-time consequences, covered under the local loop below.

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

Validation *runs* on `Pack`, so no `CP` diagnostic can appear during `dotnet build` or `dotnet test` and their cost is unchanged. Agents and developers opt in with:

```shell
dotnet pack ./Light.PortableResults.slnx -c Release
```

No properties, no script, no separate target — the same command CI runs, and on a warm NuGet cache it completes in a few seconds. Document it in `AGENTS.md` next to the existing build and test guidance.

Describe success by exit code and the absence of `CP` diagnostics, never by asserting expected output. Pack is incremental, so the console is not a reliable success signal: a first run prints restore lines, build lines and `Successfully created package` lines, a second run with no source changes prints build lines and no package lines at all, and a `--no-build` repeat on an up-to-date tree prints *nothing whatsoever* and exits 0. An agent told to look for `Successfully created package` would read that silent, correct run as a failure. Measured, on a clean tree: `--no-build` produces zero output lines and exit 0; with a break present, exit 1 and `error CP0011: ...` naming the API.

Incremental packing does not weaken the gate. Verified by changing `MetadataKind.String` from `4` — a member that exists in the 0.6.0 baseline — on an already-packed, up-to-date tree: the break is reported on the next `dotnet pack` and on the next `--no-build` pack. Note when constructing such a check that most `MetadataKind` members are new in 0.7.0, so altering them proves nothing; the baseline only contains `Null`, `Boolean`, `Int64`, `Double`, `String`, `Array` and `Object`.

Remove the non-packable warning rather than documenting around it. Packing the solution today prints:

```
warning : This project cannot be packaged because packaging has been disabled. ...
  [samples/NativeAotMovieRating/NativeAotMovieRating.csproj]
```

`Microsoft.NET.Sdk.Web.ProjectSystem.props` sets `WarnOnPackingNonPackableProject` to `true` for web projects, which the sample is; the other non-packable projects use non-web SDKs and stay quiet. Setting the property to `false` in the sample silences it. This is worth fixing on its own account — the warning is in the release workflow's pack output today — and it is what leaves a correct `--no-build` run genuinely silent instead of showing a lone warning as its only output.

Baseline *acquisition* is a different matter and does reach the ordinary loop. The SDK injects the `PackageDownload` from an evaluation-time `ItemGroup` in `Microsoft.NET.ApiCompat.targets`, not from a target, so it participates in restore whether or not `Pack` ever runs. A plain `dotnet restore` — and therefore `dotnet build` or `dotnet test` with implicit restore — fetches all seven 0.6.0 packages. Do not describe the inner loop as untouched; the accurate statement is that it is untouched *after restore*.

That cost is one cold download of seven small packages, cached thereafter, so it does not justify a redesign. Gating the baseline on whether packing is underway would mean depending on the undocumented `_IsPacking` property and would leave the CI restore without the baselines that the `--no-build` pack step needs. Accept the download and document the opt-out instead.

The existing CI cache does not cover it without a change, and this is not self-correcting. `.github/actions/cache-nuget/action.yml` keys on `hashFiles('**/packages.lock.json')`, and the baseline `PackageDownload` provably does not alter those files, so the key is unchanged by this work. `actions/cache` skips its post-job save on an exact key hit, so the pre-existing entry — which predates the baseline and does not contain the seven packages — would be restored on every run, the baselines re-downloaded every time, and the entry never refreshed. This affects every run, not only fresh runners, and it applies to `release-on-nuget.yml` too, which uses the same composite action.

Add the file that declares the baseline to the key:

```yaml
key: nuget-${{ runner.os }}-${{ hashFiles('**/packages.lock.json', 'src/Directory.Build.props') }}
```

That is the correct dependency set rather than a workaround: lock files describe what `PackageReference` resolves to, and `src/Directory.Build.props` is where `PackageValidationBaselineVersion` lives, so together they cover everything restore fetches. It rotates the key once when this change lands, so the refreshed entry is saved with the baselines in it, and rotates again when the baseline moves to 0.7.0 at release. Do not hard-code the baseline version into the cache key instead: it would duplicate a value that already exists in the props file and would silently go stale the first time someone bumps one without the other. The occasional unnecessary cache rebuild when that props file changes for an unrelated reason is the accepted cost.

The same `ItemGroup` is conditioned on `DisablePackageBaselineValidation`, so one property covers both halves:

```shell
dotnet build ./Light.PortableResults.slnx -c Release -p:DisablePackageBaselineValidation=true
```

Passed as a global property it suppresses the download at restore and the baseline comparison at pack. It does *not* disable package validation: `RunPackageValidation` still executes and still performs the compatible-framework checks, which is the desirable outcome — a `netstandard2.0`-only public type is reported as `CP0001` with the baseline disabled, so the multi-targeted packages keep their asset-compatibility gate offline. Describe it as disabling baseline validation, never as disabling validation. Verified with the baseline packages purged from the cache: restore fetches nothing, and build and the full test suite still pass.

One interaction has to be handled or the escape hatch fails at pack. With baseline validation off, the two committed baseline suppressions are never matched, and unnecessary suppressions are an error by default, so `dotnet pack -p:DisablePackageBaselineValidation=true` fails with `Unnecessary suppressions found` — an offline developer would see a hard error on an otherwise correct tree. Tie the permission to the escape hatch rather than granting it globally:

```xml
<!-- Exact. Global permission would defeat the stale-suppression detection the release handover relies on. -->
<ApiCompatPermitUnnecessarySuppressions Condition="'$(DisablePackageBaselineValidation)' == 'true'">true</ApiCompatPermitUnnecessarySuppressions>
```

Verified both directions: offline pack then succeeds with the one property, and an ordinary pack still fails on a stale suppression entry. This belongs in the `AGENTS.md` note as the offline escape hatch, worded so it is not mistaken for a way to silence a genuine break.

Lock files are not involved. The baseline `PackageDownload` does not appear in `packages.lock.json` even after `dotnet restore --force`, and locked-mode restore under `ContinuousIntegrationBuild=true` succeeds unchanged — so there is no lock file maintenance and no `NU1004` risk. Determinism does not depend on the lock file either, because the injected download pins an exact version range. A lock file diff appearing during implementation means something else moved.

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
- Making the sample packable, or removing it from the solution. Only its warning is silenced.
- Making behavioral breaks enforceable. ApiCompat cannot see them and the release notes remain the record.
- The remaining v0.7.0 preparations. This is item 3 of #77; Native AOT compatibility is #78 and the default-result write guard is #80.
