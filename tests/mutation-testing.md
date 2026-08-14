# Mutation testing with Stryker.NET

The root `AGENTS.md` is the single source for feedback-loop commands. This document explains the Stryker configuration, report interpretation, baselines, and known limitations.

## Configuration and reports

`dotnet tool restore` installs the pinned Stryker.NET 4.16.0. Mutation testing is local and on demand, with no CI or score gate. Always run from the repository root: otherwise Stryker misses `stryker-config.json`, falls back to the `vstest` runner and per-test coverage analysis, and can report all mutants as survived or fabricate `NoCoverage` results.

The shared configuration selects the MTP runner, disables unreliable per-test coverage analysis, uses Debug because warnings are errors only in Release, pins concurrency at 8, and adds 30,000 ms of timeout headroom. `additional-timeout` is config-file-only in 4.16.0 and is added to the duration derived from the initial test run; it is not the total timeout. Revisit `coverage-analysis: off` when Stryker's MTP per-test coverage support ([#3516](https://github.com/stryker-mutator/stryker-net/pull/3516)) ships and proves reliable.

Mutation runs disable public signing through the environment because Stryker 4.16.0 cannot re-emit assemblies using the committed public-only key (`CS7032`). `src/Directory.Build.props` deliberately lets an externally supplied `UsePublicSigningKey` value override its default.

`-p` selects the mutated project, not a test project. Stryker runs every test project that transitively references it, so cross-project kills are legitimate. Do not pass `-tp`, which does not narrow this test set, or `--since`, because a non-C# file in the diff degrades it to a full run.

Reports are written to `StrykerOutput/<timestamp>/reports/`: JSON is intended for automated inspection and HTML for humans. In JSON, inspect both `Survived` and `Timeout` statuses. `NoCoverage` must remain zero; a non-zero count means `coverage-analysis: off` was not applied. Compare complete count vectors at equal concurrency rather than percentages.

For a one-file configuration smoke check, mutate `**/Result.cs` in `Light.PortableResults.csproj`. The current expected vector is 2,675 tests, 67 killed, zero `NoCoverage`, and a 100% score. Update this vector after intentional changes to `Result.cs` or its tests. A 0% score for every mutant usually indicates fallback to `vstest`.

## Cost and measured baselines

Budget approximately three seconds per mutant at concurrency 8 for the large projects. Running without `-p` mutates the whole solution and has taken about seven hours; `Light.PortableResults` alone has taken about 3.6 hours. Split large projects by folder-oriented mutate globs such as `Metadata/`, `Http/`, `CloudEvents/`, and `Numbers/`.

| Project | Mutants | CompileError |
| --- | ---: | ---: |
| `Light.PortableResults` | 4,867 | 520 |
| `Light.PortableResults.Validation` | 2,215 | 83 |
| `Validation.OpenApi.SourceGeneration` | 1,272 | 204 |
| `AspNetCore.OpenApi` | 848 | 65 |
| `Validation.OpenApi` | 114 | 2 |
| `AspNetCore.Shared` | 47 | 3 |
| `AspNetCore.Mvc` | 33 | 11 |
| `AspNetCore.MinimalApis` | 32 | 11 |

The following baselines were measured with Stryker.NET 4.16.0, Debug, concurrency 8, 30,000 ms additional timeout, and an Apple M3 Max with 16 logical cores. Issue provenance refers to the change that produced the row; find it with `git log --grep "Closes #<n>"`.

| Project | Provenance | Tests | Elapsed | Killed | Timeout | Survived | CompileError | Ignored | NoCoverage |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| `AspNetCore.Mvc` | `#66` | 103 | 0:43 | 17 | 0 | 0 | 11 | 5 | 0 |
| `AspNetCore.MinimalApis` | `04aee20` | 237 | 1:22 | 16 | 0 | 0 | 11 | 5 | 0 |
| `AspNetCore.Shared` | `#80` | 415 | 2:44 | 34 | 0 | 0 | 3 | 10 | 0 |
| `Validation.OpenApi` | `#66` | 165 | 2:20 | 59 | 0 | 14 | 2 | 39 | 0 |

The fourteen `Validation.OpenApi` survivors are accounted for: twelve exercise the `target`-provided branch of typed helpers deferred to issue #57, and two are false static-initializer results described below. Its remaining ignored mutants are Stryker's deterministic block-removal filter or narrow source suppressions. Inspect ignore reasons rather than relying only on the count.

## Known blind spots

- **Compile errors and Safe Mode:** About 9.5% of the baseline mutants fail to compile, mostly around `out` and `ref`. Safe Mode then removes all mutants in the enclosing method. Observed blind spots include `Dragon4.GenerateDigits`, `ResultJsonReader.ReadStatusValue` and `ReadIndexValue`, `ErrorsExtensions.WriteRichErrors`, and all of `LightResult.cs`. Recover current sites from JSON entries with `status: CompileError`; when changing one, manually map promised behaviors to tests and explain that adequacy in the pull request.
- **Static initialization:** Mutants reached only by static initializers or constructors can appear to survive because the reused test host initializes the type before mutant activation. Verify them by applying the mutation to source and running the affected test project before adding tests. This behavior was confirmed for the `BuiltInValidationErrorContracts` registry and schema ID.
- **Unreachable cancellation:** `MetadataValueReconstructor` excludes `OperationCanceledException` in two catch filters, but Roslyn intercepts pre-cancelled tokens before reconstruction and the accepted framework evaluations cannot throw cancellation. The distinction becomes testable only if an accepted evaluation gains a reachable cancellation path.
- **Timeout classification:** Stryker counts timeouts as killed, but classification depends on hardware and load. The configured headroom eliminated known false timeouts in repeated baseline runs; investigate every future timeout as a possible hang or insufficient headroom.
- **Preview runner:** The MTP runner remains a preview integration ([#3094](https://github.com/stryker-mutator/stryker-net/issues/3094)). Verify surprising results with a plain `dotnet test` run.
