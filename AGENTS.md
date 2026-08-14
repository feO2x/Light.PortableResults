# Root Agents.md

Light.PortableResults is a lightweight, high-performance library implementing the Result Pattern for .NET. It stands out for reducing allocations and being able to serialize and deserialize results across different protocols (HTTP via RFC-9457, gRPC, Asynchronous Messaging). Extensibility is less important than performance.

## When you implement a plan

Plans in `ai-plans/` are frozen after their Planning Phase. The only permitted change to a frozen plan is checking an acceptance criterion (`- [ ]` to `- [x]`), and only after the implementation and the relevant feedback loops verify it. Leave unmet criteria unchecked and never change their wording. If the implementation materially departs from an explicit plan decision, write a Plan Deviations document instead of editing the frozen plan.

## Feedback loops

Run all commands from the repository root.

- `dotnet test Light.PortableResults.slnx` - builds the solution and runs all test projects. See ./tests/AGENTS.md for how to write tests.
- `dotnet test Light.PortableResults.slnx -- --coverage --coverage-settings "$PWD/coverage.runsettings" --coverage-output-format cobertura` - additionally collects line coverage. Merge the per-project reports with `reportgenerator -reports:'**/*.cobertura.xml' -targetdir:./coverage-merged -reporttypes:'Cobertura;TextSummary'`. Line coverage must stay at or above 95%.
- `dotnet pack ./Light.PortableResults.slnx -c Release` - Release build (warnings are errors) plus package validation of the public API shape against the published baseline. This is exactly the CI gate; see Package Validation for details and the offline escape hatch.
- `dotnet test ./tests/Light.PortableResults.Tests/Light.PortableResults.Tests.csproj --configuration Release -p:PortableResultsAssetTargetFramework=netstandard2.0` - CI gate that runs the core test suite against the netstandard2.0 asset.
- `dotnet publish ./samples/NativeAotMovieRating/NativeAotMovieRating.csproj -c Release -r linux-x64` - CI gate for the Native AOT compatibility claim. Requires a platform linker (clang/gcc), see https://aka.ms/nativeaot-prerequisites.
- Benchmarks: `dotnet run -c Release --project benchmarks/Benchmarks -- --filter <glob>` (BenchmarkDotNet switcher). Run only for changes where performance is a material risk or requirement.
- Mutation testing: `dotnet tool restore`, then `UsePublicSigningKey=false dotnet stryker -p <project>` from the repository root (the environment variable opts out of public signing, which Stryker 4.16.0 cannot re-emit). Local, on-demand use only; see ./tests/AGENTS.md for configuration, baselines, and triage rules.

No dependency, secret, container, or source-code security scans are configured in this repository.

## General Rules for the Code Base

In our Directory.Build.props files in this solution, the following rules are defined:

- The library is not published in a stable version yet, you can make breaking changes.
- When a type or method is properly encapsulated, make it public. We don't know how callers would like to use this library. When some types are internal, this might make it hard for callers to access these in tests or when making configuration changes. Prefer public APIs over internal ones.
- Use Conventional Commits messages. Decide whether a commit title is enough or a commit body is required.

## Package Validation

Every packable project under `src/` is validated against its published `0.7.0` package, so an undeclared change to the public API shape fails the build instead of reaching review as prose. The local gate is exactly the CI gate:

```shell
dotnet pack ./Light.PortableResults.slnx -c Release
```

No extra arguments, properties or scripts. Run it before pushing an API change.

Judge the result by the exit code, not by the console text: an incremental run may print package lines, build lines, or nothing at all, because the validation target skips when its inputs are unchanged. A non-zero exit and a `CP` diagnostic naming the affected API mean a break was found:

- `CP0001`/`CP0002` - a type or member that `0.7.0` shipped is gone, or the `net10.0` asset is missing API that `netstandard2.0` has.
- `CP0003` - the assembly identity changed, in practice the strong-name key.
- `CP0011` - an enum field changed its numeric value.

If the break is intentional, say so in `PackageReleaseNotes` and regenerate the suppression file with `/p:ApiCompatGenerateSuppressionFile=true`. Review every generated entry: the tool suppresses whatever it finds, including the break you did not mean to make. Unnecessary suppressions fail the build, so a suppression file never silently outlives the break it covers.

ApiCompat compares assembly shape only. Behavioral breaks - encoding changes, new guard clauses, changed exception types - are invisible to it and stay the responsibility of tests and release notes.

The baseline packages are acquired by a `PackageDownload` that is added during evaluation, so **every** restore fetches them, not just `pack`. A cold restore therefore pays one download of seven small packages; compilation and test execution afterwards are unaffected. `PackageDownload` does not write to `packages.lock.json`, which is why the CI cache key also hashes `src/Directory.Build.props`.

To work offline, or whenever the baseline packages cannot be reached:

```shell
dotnet build ./Light.PortableResults.slnx -c Release -p:DisablePackageBaselineValidation=true
```

The property works for `restore`, `build`, `test` and `pack`. It skips the baseline download and the baseline comparison, and it permits the then-unmatched baseline suppressions. Validation against the compatible frameworks inside the package stays active, so a `net10.0` asset that loses API relative to `netstandard2.0` still fails. Do not commit work verified only this way - the baseline comparison is the part that was switched off.

Ordinary `src` builds are public-signed with the committed `Light.PortableResults.Public.snk` so that their identity matches the strong-named `0.7.0` baselines. This is set in `src/Directory.Build.props` rather than passed on the command line, precisely so that the plain `dotnet pack` above is the whole gate. Public signing carries the strong-name identity without a valid signature, which is why packages must only ever be pushed by the release workflow: it supplies the private key and produces genuinely signed assemblies.

### After a release

Once a version is published, raise `PackageValidationBaselineVersion` in `src/Directory.Build.props` to it, bump `Version` in the root `Directory.Build.props` to the next patch, empty every `PackageReleaseNotes`, and delete any `CompatibilitySuppressions.xml` under `src/` in the same commit. A suppression only ever covers a break against the previous baseline; against the new one it is unnecessary, and unnecessary suppressions fail the build. Each cycle therefore starts from a clean baseline with no suppression file.

The `0.7.0` cleanup was done this way: the two intentional `MetadataKind.Array`/`MetadataKind.Object` renumberings from the `0.6.0` era stopped being breaks, so `src/Light.PortableResults/CompatibilitySuppressions.xml` was removed.

## Testing Rules

Read ./tests/AGENTS.md for details about how to write tests.

## How to write plans

Read ./ai-plans/AGENTS.md for details on how to write plans.

## This is your space

If you encounter something worth noting while you are working on this code base, write it down here in this section. Once you are finished, I will discuss it with you, and we can decide where to put your notes.
