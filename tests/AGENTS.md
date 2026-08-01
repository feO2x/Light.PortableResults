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

`dotnet tool restore` installs the pinned `dotnet-stryker` 4.16.0; the shared config is `stryker-config.json` at the repository root. Local, on-demand use only: no CI integration and no score gates (`thresholds`/`break-at` are unset). A surviving mutant is a signal to investigate, not proof of a defect.

### How to run

Always run from the repository root: Stryker reads `stryker-config.json` only from the current directory and silently falls back to defaults without it, reverting to the `vstest` runner (cannot drive the xUnit v3 hosts, reports everything survived) and `perTest` coverage analysis (fabricates `NoCoverage`). The config also pins `Debug` (`TreatWarningsAsErrors` is Release-only), `concurrency: 8` (result vectors vary with parallelism; override with `-c` for experiments), and `additional-timeout: 30000` (the 5,000 ms default masks slow survivors as timeouts under load).

`additional-timeout` is config-file-only in 4.16.0 and is milliseconds of headroom added to Stryker's timeout derived from the initial test run, not the total timeout. Raising it makes genuine hangs take longer to classify, but the measured value exposes all known slow survivors while keeping the four small-project runs within minutes.

Revisit `coverage-analysis: off` when Stryker's MTP per-test coverage support ([#3516](https://github.com/stryker-mutator/stryker-net/pull/3516)) ships and proves trustworthy; it is the main lever on run time because it avoids running the full discovered test set for every mutant.

```shell
# One project — sufficient for the four small projects
dotnet stryker -p Light.PortableResults.AspNetCore.Shared.csproj

# One-file configuration smoke check (~4 minutes)
# Expect: 2,398 tests, 67 killed, 0 NoCoverage, 100.00%
dotnet stryker -p Light.PortableResults.csproj -m '**/Result.cs'
```

The smoke-check vector is tied to the current `Result.cs` and its tests; update it after intentional changes alter the mutant inventory. All mutants surviving with a 0.00% score suggests fallback to the `vstest` runner, while any non-zero `NoCoverage` count suggests `coverage-analysis: off` was lost.

`-p` selects the mutated project, not the tests: Stryker runs every test project transitively referencing it (`AspNetCore.Shared` → 337 tests, `Light.PortableResults` → all 2,398). Cross-project kills are legitimate (sociable tests) and nearly free. Never pass `-tp` (it does not narrow tests) or `--since` (any non-C# file in the diff, e.g. an `ai-plans/` document, degrades it to a full run). Reports go to `StrykerOutput/<timestamp>/reports/` (gitignored): JSON for agents (filter `"status"` for both `"Survived"` and `"Timeout"`; investigate the timeout cause before survivor triage), HTML for humans.

### Cost and baseline

~3 s per mutant at concurrency 8 (upper bound from the large projects). Omitting `-p` mutates the whole solution (~7 h; `Light.PortableResults` alone ~3.6 h) — split large projects by mutate glob along folder boundaries (`Metadata/`, `Http/`, `CloudEvents/`, `Numbers/`). Mutant inventory for sizing:

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

Baseline for sources at commit `04aee20`, remeasured with `dotnet-stryker` 4.16.0, concurrency 8 and 30,000 ms additional timeout (both pinned in `stryker-config.json`), `Debug`, Apple M3 Max (16 logical cores):

| Project | Tests run | Elapsed | Killed | Timeout | Survived | CompileError | Ignored | NoCoverage |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| `AspNetCore.Mvc` | 102 | 0:53 | 16 | 0 | 1 | 11 | 5 | 0 |
| `AspNetCore.MinimalApis` | 237 | 1:22 | 16 | 0 | 0 | 11 | 5 | 0 |
| `AspNetCore.Shared` | 337 | 2:29 | 31 | 0 | 0 | 3 | 10 | 0 |
| `Validation.OpenApi` | 163 | 2:38 | 57 | 0 | 19 | 2 | 36 | 0 |

`Validation.OpenApi` was run twice consecutively with the 30,000 ms setting; both runs produced 57 killed, 0 timeout, 19 survived, and a 75.00% score, completing in 2:32 and 2:38. At 20,000 ms, one of two runs still timed out a mutant known to survive. At the 5,000 ms default, 21 mutants were reported as timeouts, masking most survivors and inflating the score to 98.68%.

`Ignored` is not user suppression: all 56 baseline entries are `Block removal` mutants discarded deterministically by Stryker's built-in "block already covered" filter because another active mutant exists inside the block. A different reason or count should be investigated.

`NoCoverage` must be zero — a non-zero value means `coverage-analysis: off` is no longer taking effect. Compare full count vectors at equal concurrency, not percentages.

### Triaging survivors

Exactly three permitted responses to a survivor:

1. **Behavior is genuinely unconstrained** — add or strengthen a test (the common case).
2. **Equivalent or invalid mutant** — suppress narrowly at the source with a justification: `// Stryker disable once Statement : equivalent - <reason>`. Prefer `disable once` over `disable all`; never use the global `ignore-mutations` setting for a single site.
3. **Untestable construct** — record it and move on (blind spots below).

Never restructure production code to make a mutant killable: performance outranks mutation score here, and the low-allocation `in`/`ref`/`Span` style produces awkward survivors by nature.

### Blind spots — do not read as adequate coverage

- ~9.5% of mutants fail to compile (mostly the `out`/`ref` style); Stryker's Safe Mode then discards every mutant in the enclosing method: `Dragon4.GenerateDigits`, `ResultJsonReader.ReadStatusValue`/`ReadIndexValue`, `ErrorsExtensions.WriteRichErrors`, and all of `LightResult.cs` receive no mutation coverage. Tool limitation, not a test defect — a high score in `Numbers/` is not verified behavior.
- `Timeout` counts as killed. The pinned 30,000 ms additional timeout reduced `Validation.OpenApi` from 21 timeouts to zero in two consecutive concurrency-8 runs, but no finite value makes classification independent of hardware and load. Investigate any future timeout as either a genuine hang or insufficient headroom; do not assume it represents a killed mutant.
- The MTP runner is a preview (stryker-mutator/stryker-net#3094); verify surprising results against a plain `dotnet test` run.
