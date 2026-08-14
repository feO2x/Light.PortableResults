# Repository instructions

Light.PortableResults is a high-performance Result Pattern library for .NET with typed round trips, e.g., for RFC 9457 HTTP responses and CloudEvents. Prioritize low allocations and throughput over extensibility.

## When you implement a plan

Plans in `ai-plans/` are frozen after their Planning Phase. The only permitted change to a frozen plan is checking an acceptance criterion (`- [ ]` to `- [x]`), and only after the implementation and relevant feedback loops verify it. Leave unmet criteria unchecked and never change their wording. If the implementation materially departs from an explicit plan decision, write a Plan Deviations document instead of editing the frozen plan.

## Feedback loops

Run all commands from the repository root.

- `dotnet test Light.PortableResults.slnx` - builds the solution and runs all test projects.
- `dotnet test Light.PortableResults.slnx -- --coverage --coverage-settings "$PWD/coverage.runsettings" --coverage-output-format cobertura` - collects line coverage. Merge the reports with `reportgenerator -reports:'**/*.cobertura.xml' -targetdir:./coverage-merged -reporttypes:'Cobertura;TextSummary'`; the merged result must remain at or above 95%.
- `dotnet pack ./Light.PortableResults.slnx -c Release` - performs the Release build with warnings as errors and validates package APIs against the published baseline. This is the CI gate; see Package validation for intentional breaks and offline work.
- `dotnet test ./tests/Light.PortableResults.Tests/Light.PortableResults.Tests.csproj --configuration Release -p:PortableResultsAssetTargetFramework=netstandard2.0` - runs the core suite against the `netstandard2.0` asset.
- `dotnet publish ./samples/NativeAotMovieRating/NativeAotMovieRating.csproj -c Release -r linux-x64` - validates the Native AOT compatibility claim. It requires a platform linker such as clang or gcc.
- `dotnet run -c Release --project benchmarks/Benchmarks -- --filter <glob>` - runs selected benchmarks. Use it only when performance is a material risk or requirement.
- `dotnet tool restore`, then `UsePublicSigningKey=false dotnet stryker -p <project>` - runs mutation testing for a selected project. This is local and on demand; see `tests/mutation-testing.md` for configuration, baselines, triage, and known blind spots.

No dependency, secret, container, or source-code security scans are configured.

## Project conventions

- Breaking changes are permitted before 1.0, but they must follow the package-validation and release-note requirements below.
- Prefer public APIs over internal ones when a type or member is properly encapsulated, so callers can configure and test the library without workarounds. See https://blog.ploeh.dk/2015/09/21/public-types-hidden-in-plain-sight/
- Use Conventional Commits; add a body when the title does not adequately explain the rationale.

## Package validation

Every packable project under `src/` is validated against the version in `PackageValidationBaselineVersion` in `src/Directory.Build.props`. The package feedback loop above is authoritative; judge it by its exit code and run it before pushing an API change.

For an intentional break, describe it in `PackageReleaseNotes`, regenerate suppressions with `/p:ApiCompatGenerateSuppressionFile=true`, and review every generated entry. Unnecessary suppressions fail validation. ApiCompat checks assembly shape, not behavioral breaks such as changed encodings, guards, or exception types; cover those with tests and release notes.

When the baseline packages are unavailable, use `dotnet build ./Light.PortableResults.slnx -c Release -p:DisablePackageBaselineValidation=true`. This skips only the published-baseline comparison; never treat a build verified solely this way as final verification.

Ordinary `src` builds are public-signed to match the published assembly identity. Only the release workflow supplies the private key, so never publish packages produced by an ordinary local or CI build.

### After a release

In one commit, set `PackageValidationBaselineVersion` to the released version, bump the root `Version` to the next patch, empty every `PackageReleaseNotes`, and delete all `CompatibilitySuppressions.xml` files under `src/`.

## How to write plans

Follow `ai-plans/AGENTS.md` and use the corresponding Guided Coding skill before writing a plan or Plan Deviations document.

## This is your space

Found something noteworthy during implementation? Write down your discoveries here, we will discuss them afterward.
