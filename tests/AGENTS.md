# AGENTS.md for Tests

## General Rules

- Please do not use mocking frameworks like Moq or NSubstitute for test doubles, use hand-crafted Test Doubles instead.
- Do not write nested test classes. All tests should reside in a class which is directly placed in a namespace.
- Use PascalCase for test method names without underscores (e.g., `ParseThrowsForInvalidInput`).
- Use FluentAssertions instead of xunit's `Assert` class.
- When writing Unit Tests (i.e., tests that only run in-memory and make no I/O calls to third-party systems), prefer Sociable Tests instead of Solitary Tests (according to Martin Fowler's definition). Create as much test coverage as possible by calling higher level production APIs. Only write Solitary Tests to cover otherwise unreachable lower level APIs – for example, Guard Clauses.
- During Integration Tests, at least one I/O call to third-party systems like a database or Web API is made. Some of the third-party system calls can be replaced with Test Doubles or Fakes (according to XUnit Test Patterns by Gerard Meszaros).
- In End-to-End (E2E) Tests, I/O calls must not be replaced with Test Doubles or Fakes.
- Keep Code Coverage at least above 95%. Use Microsoft.Testing.Extensions.CodeCoverage to measure it.

## How to run

- `dotnet test Light.PortableResults.slnx` for regular test runs.
- `dotnet test Light.PortableResults.slnx -- --coverage --coverage-settings "$PWD/coverage.runsettings" --coverage-output-format cobertura` for test coverage metrics.

Always pass `--coverage-settings`. It excludes source-generated files under `obj/`, which otherwise dominate the line counts and report the solution at roughly 81% instead of 95%. The path must be absolute, because each test app runs with its own output directory as the working directory.

Each test project writes `TestResults/<guid>.cobertura.xml`. Merge them with `reportgenerator -reports:'**/*.cobertura.xml' -targetdir:./coverage-merged -reporttypes:'Cobertura;TextSummary'`.

## Mutation testing (Stryker.NET)

Stryker.NET is pinned as a local tool (`dotnet tool restore` installs `dotnet-stryker` 4.16.0) with the shared configuration committed in `stryker-config.json` at the repository root. It is a local, on-demand tool only: there is no CI integration and no mutation-score gate (`thresholds` and `break-at` are deliberately unset). A surviving mutant is a signal to investigate, not proof of a defect.

### How to run

Always run from the repository root. Stryker resolves `stryker-config.json` from the current working directory and a missing config is not an error — it silently falls back to the defaults, which lose the two load-bearing settings at once (the MTP test runner and the disabled coverage analysis) and produce confidently wrong reports: 0.00% scores under the `vstest` runner, or fabricated `NoCoverage` under `perTest` coverage analysis.

```shell
# One project — the only form needed for the four small projects
dotnet stryker -p Light.PortableResults.AspNetCore.Shared.csproj

# One file, via an optional mutate glob (about four minutes for Result.cs)
dotnet stryker -p Light.PortableResults.csproj -m '**/Result.cs'
```

The committed config names the solution, so every run is a solution-context run: `-p` selects which source project is mutated, but it does not narrow which tests execute. Stryker runs **every test project that transitively references the mutated project** — mutating `AspNetCore.Shared` executes 337 tests (all six referencing test projects, not the 26 of `AspNetCore.Shared.Tests` alone), and mutating `Light.PortableResults` executes all 2,398. Keep this: a mutant killed by another project's tests is legitimately killed (sociable tests), and the extra tests are nearly free because per-mutant cost is dominated by fixed overhead. `-tp` does not override this behavior and must not be passed as if it did. Do not use `--since` either: it treats any non-C# file in the diff (a plan under `ai-plans/` qualifies) as a changed test file and degrades to a full project run while presenting itself as scoped.

The config pins `test-runner: mtp` because the default `vstest` runner cannot drive the xUnit v3 hosts used here (coverage capture fails and every mutant is reported survived), `coverage-analysis: off` because the MTP per-test analysis fabricates `NoCoverage` for members the tests demonstrably cover, and `configuration: Debug` so that the Release-only `TreatWarningsAsErrors` cannot interfere with Stryker's rollback. MTP support is a Stryker preview feature (stryker-mutator/stryker-net#3094); per-test coverage for MTP is pending upstream (#3516) and is the main lever on run time once it ships and proves trustworthy.

Reports are written to `StrykerOutput/<timestamp>/reports/` (gitignored). The JSON report is the agent-facing artifact — filter for `"status": "Survived"` to get a work queue located at specific files and lines; the HTML report is for humans.

### Cost and scope

Measured throughput is about three seconds per mutant at concurrency 8 (upper bound, taken from the large projects). Never mutate the whole solution in the inner loop: omitting `-p` is the seven-hour path. `Light.PortableResults` alone is ~3.6 h and `Validation` ~1.8 h; split them by mutate glob along folder boundaries (`Metadata/`, `Http/`, `CloudEvents/`, `Numbers/`) so runs stay interruptible and reports arrive while still actionable. Mutant inventory for sizing (measured at concurrency 8; the four small projects re-verified at the baseline below):

| Project | Mutants | CompileError |
| --- | ---: | ---: |
| `Light.PortableResults` | 4,867 | 520 |
| `Light.PortableResults.Validation` | 2,215 | 83 |
| `Validation.OpenApi.SourceGeneration` | 1,272 | 204 |
| `AspNetCore.OpenApi` | 848 | 65 |
| `Validation.OpenApi` | 114 | 2 |
| `AspNetCore.Shared` | 44 | 3 |
| `AspNetCore.Mvc` | 33 | 11 |
| `AspNetCore.MinimalApis` | 32 | 11 |

### Baseline

Measured on an Apple M3 Max (16 logical cores) at commit `04aee20` (source tree; the tooling change itself was uncommitted at measurement time), `dotnet-stryker` 4.16.0, concurrency pinned to 8 via `-c 8`, `Debug`:

| Project | Tests run | Elapsed | Killed | Timeout | Survived | CompileError | Ignored | NoCoverage |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| `AspNetCore.Mvc` | 102 | 0:40 | 16 | 1 | 0 | 11 | 5 | 0 |
| `AspNetCore.MinimalApis` | 237 | 0:58 | 16 | 0 | 0 | 11 | 5 | 0 |
| `AspNetCore.Shared` | 337 | 1:57 | 31 | 0 | 0 | 3 | 10 | 0 |
| `Validation.OpenApi` | 163 | 2:06 | 54 | 21 | 1 | 2 | 36 | 0 |

The `NoCoverage` column must be zero even though nothing is expected there: a non-zero value means `coverage-analysis: off` is no longer taking effect. When comparing a future run against this table, compare counts, not the percentage alone.

### Triaging survivors

A surviving mutant means the suite did not distinguish the mutated program from the original. There are exactly three permitted responses:

1. **Observable behavior is genuinely unconstrained** — add or strengthen a test. This is the outcome the tool exists to produce and should be the common one.
2. **The mutant is equivalent or invalid** — suppress it narrowly at the source with a justification, which Stryker records in the report:

   ```csharp
   // Stryker disable once Statement : equivalent - the guard is a fast path, not a behavior change
   ```

   The syntax is `Stryker [disable|restore][once][all|<mutator list>][: reason]` and is scope-aware. Prefer `disable once` over `disable all`, and never use the global `ignore-mutations` setting to silence a single site.
3. **The mutant sits in a construct this configuration cannot meaningfully test** — record it and move on; see the blind spots below.

Never restructure production code solely to make a mutant killable. Performance ranks above extensibility in this library, and the low-allocation `in`/`ref`/`Span` style is precisely the shape that produces awkward survivors; a lower mutation score is the correct outcome when the alternative is a slower implementation.

### Blind spots — do not read these as adequate coverage

- Roughly 9.5% of mutants fail to compile, concentrated in the low-allocation `out`/`ref` style. Stryker responds with Safe Mode, which discards **every** mutant in the enclosing method: `Dragon4.GenerateDigits`, `ResultJsonReader.ReadStatusValue`, `ResultJsonReader.ReadIndexValue`, `ErrorsExtensions.WriteRichErrors`, and the whole of `LightResult.cs` (11 of 11 mutants) receive no mutation coverage at all. This is a tool limitation, not a test defect — a high score in `Numbers/` is not verified behavior, and no one should "fix" it by restructuring production code.
- `Timeout` counts as killed in the mutation score, but with `coverage-analysis: off` every mutant runs the full discovered test set, so a genuinely surviving mutant whose tests all pass runs the entire suite and can exceed the per-mutant timeout — surfacing as `Timeout` instead of `Survived`, depending on machine load and concurrency. `Validation.OpenApi` shows this: its 21 timeouts at concurrency 8 mostly become 18 survivors at concurrency 4 (score drops from 98.68% to 76.32%). Treat `Timeout` as a triage signal equal to `Survived`, especially in that project.
- The MTP runner is a preview; verify surprising results against a plain `dotnet test` run before acting on them.
