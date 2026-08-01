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

Always run from the repository root: Stryker reads `stryker-config.json` only from the current directory and silently falls back to defaults without it, reverting to the `vstest` runner (cannot drive the xUnit v3 hosts, reports everything survived) and `perTest` coverage analysis (fabricates `NoCoverage`). The config also pins `Debug` (`TreatWarningsAsErrors` is Release-only) and `concurrency: 8` (result vectors vary with parallelism; override with `-c` for experiments).

```shell
# One project — sufficient for the four small projects
dotnet stryker -p Light.PortableResults.AspNetCore.Shared.csproj

# One file (~4 minutes for Result.cs)
dotnet stryker -p Light.PortableResults.csproj -m '**/Result.cs'
```

`-p` selects the mutated project, not the tests: Stryker runs every test project transitively referencing it (`AspNetCore.Shared` → 337 tests, `Light.PortableResults` → all 2,398). Cross-project kills are legitimate (sociable tests) and nearly free. Never pass `-tp` (it does not narrow tests) or `--since` (any non-C# file in the diff, e.g. an `ai-plans/` document, degrades it to a full run). Reports go to `StrykerOutput/<timestamp>/reports/` (gitignored): JSON for agents (filter `"status": "Survived"`), HTML for humans.

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

Baseline measured at commit `04aee20`, `dotnet-stryker` 4.16.0, concurrency 8 (pinned in `stryker-config.json`; the 4.16.0 default is half the logical cores and therefore machine-dependent), `Debug`, Apple M3 Max:

| Project | Tests run | Elapsed | Killed | Timeout | Survived | CompileError | Ignored | NoCoverage |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| `AspNetCore.Mvc` | 102 | 0:40 | 16 | 1 | 0 | 11 | 5 | 0 |
| `AspNetCore.MinimalApis` | 237 | 0:58 | 16 | 0 | 0 | 11 | 5 | 0 |
| `AspNetCore.Shared` | 337 | 1:57 | 31 | 0 | 0 | 3 | 10 | 0 |
| `Validation.OpenApi` † | 163 | 2:06 | 54 | 21 | 1 | 2 | 36 | 0 |

† This row is not exactly reproducible. With `coverage-analysis: off` every mutant runs the full 163-test suite, so the Killed/Timeout/Survived split is machine-load dependent (see the timeout-masking blind spot): two runs of identical sources at concurrency 8 gave Killed 51–54, Timeout 21, Survived 1–4. Tests run, CompileError, and Ignored are deterministic. For triage rather than comparison, run this project with `-c 4` to unmask the slow full-suite survivors (56 killed, 2 timeout, 18 survived).

`NoCoverage` must be zero — a non-zero value means `coverage-analysis: off` is no longer taking effect. Compare full count vectors at equal concurrency, not percentages.

### Triaging survivors

Exactly three permitted responses to a survivor:

1. **Behavior is genuinely unconstrained** — add or strengthen a test (the common case).
2. **Equivalent or invalid mutant** — suppress narrowly at the source with a justification: `// Stryker disable once Statement : equivalent - <reason>`. Prefer `disable once` over `disable all`; never use the global `ignore-mutations` setting for a single site.
3. **Untestable construct** — record it and move on (blind spots below).

Never restructure production code to make a mutant killable: performance outranks mutation score here, and the low-allocation `in`/`ref`/`Span` style produces awkward survivors by nature.

### Blind spots — do not read as adequate coverage

- ~9.5% of mutants fail to compile (mostly the `out`/`ref` style); Stryker's Safe Mode then discards every mutant in the enclosing method: `Dragon4.GenerateDigits`, `ResultJsonReader.ReadStatusValue`/`ReadIndexValue`, `ErrorsExtensions.WriteRichErrors`, and all of `LightResult.cs` receive no mutation coverage. Tool limitation, not a test defect — a high score in `Numbers/` is not verified behavior.
- `Timeout` counts as killed, but full-suite survivors can exceed the per-mutant timeout under load: `Validation.OpenApi`'s 21 timeouts at `-c 8` become 18 survivors at `-c 4` (76.32%). Treat `Timeout` like `Survived` in that project.
- The MTP runner is a preview (stryker-mutator/stryker-net#3094); verify surprising results against a plain `dotnet test` run.
