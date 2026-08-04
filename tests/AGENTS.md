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

# One-file configuration smoke check (~5 minutes)
# Expect: 2,675 tests, 67 killed, 0 NoCoverage, 100.00%
dotnet stryker -p Light.PortableResults.csproj -m '**/Result.cs'
```

The smoke-check vector is tied to the current `Result.cs` and its tests; update it after intentional changes alter the mutant inventory. All mutants surviving with a 0.00% score suggests fallback to the `vstest` runner, while any non-zero `NoCoverage` count suggests `coverage-analysis: off` was lost.

`-p` selects the mutated project, not the tests: Stryker runs every test project transitively referencing it (`AspNetCore.Shared` → 415 tests, `Light.PortableResults` → all 2,675). Cross-project kills are legitimate (sociable tests) and nearly free. Never pass `-tp` (it does not narrow tests) or `--since` (any non-C# file in the diff, e.g. an `ai-plans/` document, degrades it to a full run). Reports go to `StrykerOutput/<timestamp>/reports/` (gitignored): JSON for agents (filter `"status"` for both `"Survived"` and `"Timeout"`; investigate the timeout cause before survivor triage), HTML for humans.

### Cost and baseline

~3 s per mutant at concurrency 8 (upper bound from the large projects). Omitting `-p` mutates the whole solution (~7 h; `Light.PortableResults` alone ~3.6 h) — split large projects by mutate glob along folder boundaries (`Metadata/`, `Http/`, `CloudEvents/`, `Numbers/`). Mutant inventory for sizing:

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

Baselines carry per-row provenance, because rows are re-measured individually as survivors are triaged. A row measured as part of the change that produced it cites the issue rather than a hash the commit cannot contain; find it with `git log --grep "Closes #<n>"`. All runs used `dotnet-stryker` 4.16.0, concurrency 8 and 30,000 ms additional timeout (both pinned in `stryker-config.json`), `Debug`, Apple M3 Max (16 logical cores):

| Project | Provenance | Tests run | Elapsed | Killed | Timeout | Survived | CompileError | Ignored | NoCoverage |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| `AspNetCore.Mvc` | `#66` | 103 | 0:43 | 17 | 0 | 0 | 11 | 5 | 0 |
| `AspNetCore.MinimalApis` | `04aee20` | 237 | 1:22 | 16 | 0 | 0 | 11 | 5 | 0 |
| `AspNetCore.Shared` | `#80` | 415 | 2:44 | 34 | 0 | 0 | 3 | 10 | 0 |
| `Validation.OpenApi` | `#66` | 165 | 2:20 | 59 | 0 | 14 | 2 | 39 | 0 |

Both `Validation.OpenApi` survivor groups are accounted for and neither is a missing test: twelve are the `target`-provided branch of the typed helpers, deferred to the bug in #57 so that tests are written against the corrected contract, and two are the known-false static-initializer mutants described in the blind spots below. Its 39 `Ignored` are 36 block-removal plus the three triage suppressions.

At the 5,000 ms default additional timeout, `Validation.OpenApi` reported 21 mutants as timeouts, masking most of these survivors and inflating the score to 98.68%. At 20,000 ms, one of two runs still timed out a mutant known to survive.

`Ignored` covers two distinct things, and the report carries the reason for each. Most entries are `Block removal` mutants discarded deterministically by Stryker's built-in "block already covered" filter because another active mutant exists inside the block. The rest are `Stryker disable once` suppressions from triage, which carry the justification written at the source. Check the reasons, not just the count: a `Block removal` count that moves without a source change should be investigated.

`NoCoverage` must be zero — a non-zero value means `coverage-analysis: off` is no longer taking effect. Compare full count vectors at equal concurrency, not percentages.

### Triaging survivors

Exactly three permitted responses to a survivor:

1. **Behavior is genuinely unconstrained** — add or strengthen a test (the common case).
2. **Equivalent or invalid mutant** — suppress narrowly at the source with a justification: `// Stryker disable once Statement : equivalent - <reason>`. Prefer `disable once` over `disable all`; never use the global `ignore-mutations` setting for a single site.
3. **Untestable construct** — record it and move on (blind spots below).

Never restructure production code to make a mutant killable: performance outranks mutation score here, and the low-allocation `in`/`ref`/`Span` style produces awkward survivors by nature.

The same discipline applies to test code. A test written under response 1 must stand on its own as a statement about the contract: name it for the behavior it pins down, and assert only on what the public API promises. Nothing in a test may refer to a mutant — no mutant ID, no line number, no mention of Stryker in a name, comment, or assertion message. The source suppression from response 2 is the only place a mutant is named.

If a survivor can only be killed by asserting on something incidental — exact message composition, member or property ordering, a call count, a value the contract does not fix — it is response 2, not response 1. Suppress it with that reasoning. Such a test raises the score once and then constrains an implementation detail forever, which is a worse position than the survivor: the next legitimate refactoring breaks it, and the failure carries no information about the contract.

### Blind spots — do not read as adequate coverage

- ~9.5% of mutants fail to compile (mostly the `out`/`ref` style); Stryker's Safe Mode then discards every mutant in the enclosing method, which receives no mutation coverage at all. Tool limitation, not a test defect — a high score in `Numbers/` is not verified behavior. Observed at the baseline: `Dragon4.GenerateDigits`, `ResultJsonReader.ReadStatusValue`/`ReadIndexValue`, `ErrorsExtensions.WriteRichErrors`, and all of `LightResult.cs` (11 of 11 mutants).

  Treat that list as observed, not fixed: any new `out`/`ref` code joins it silently. Stryker announces it as `[INF] Safe Mode! Stryker will remove all mutations in <method>` on the console only — no log file is written — and the discarded mutants are simply absent from the JSON report. The durable way to recover the current set is to filter the report for `"status": "CompileError"`; those sites are the only trace left, and their enclosing methods are the ones running blind.

  When changing a method in that set, mutation score carries no information about it and line coverage only proves execution. Adequacy has to be argued by hand: enumerate the behaviors the method promises and point at the test constraining each one. State that reasoning in the pull request, because no tool in this repository can check it.
- Mutants reachable only during static initialization are reported as survived even when the suite kills them. Measured on `BuiltInValidationErrorContracts`: emptying the `Contracts` registry fails 46 of 112 tests and blanking the built-in schema id string fails 52 of 112, yet Stryker reported both as `Survived`. Both sites run only while a static property initializer executes, which a reused test host runs once per process, independently of mutant activation. Verify any survivor in a static initializer, a static constructor, or a helper called only from one by applying the mutation to the source by hand and running the affected test project — the report cannot settle it. Do not add tests for such a survivor before that check: the two above already had covering assertions.
- `MetadataValueReconstructor` keeps `OperationCanceledException` out of its two evaluation catch filters, and that contract cannot be observed through the generator's public surface: Roslyn intercepts a pre-cancelled token before reconstruction runs, and none of the whitelisted framework constructors and factories can throw cancellation. No test can distinguish the filter from a plain `catch (Exception)`, so the contract rests on the filters' structure. It becomes testable — and needs a test — as soon as an accepted evaluation gains a reachable cancellation path.
- `Timeout` counts as killed. The pinned 30,000 ms additional timeout reduced `Validation.OpenApi` from 21 timeouts to zero in two consecutive concurrency-8 runs, but no finite value makes classification independent of hardware and load. Investigate any future timeout as either a genuine hang or insufficient headroom; do not assume it represents a killed mutant.
- The MTP runner is a preview (stryker-mutator/stryker-net#3094); verify surprising results against a plain `dotnet test` run.
