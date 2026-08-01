# Stryker.NET Mutation Testing

## Rationale

Line coverage is held above 95%, but it only proves that code executed, not that any assertion constrains it. Coding agents extending this library cannot currently distinguish a well-tested code path from one that is merely reached. Mutation testing narrows that gap: a surviving mutant is a concrete, machine-readable location where the suite failed to detect a changed program, which makes it a candidate for a missing assertion rather than proof of one. Some survivors are equivalent or not worth killing, so the output is a triage queue, not a defect list.

Introduce Stryker.NET as a pinned, opt-in tool with a repository configuration that is correct by default. Two settings are load-bearing — the wrong test runner and the default coverage analysis each produce confidently wrong reports on this solution — so the configuration, not the invocation, must carry them.

## Acceptance Criteria

- [ ] `dotnet tool restore` installs a version-pinned `dotnet-stryker`, and a repository-root `stryker-config.json` supplies the shared configuration so that a scoped run needs only project and test-project arguments.
- [ ] The committed configuration selects the Microsoft Testing Platform runner and disables coverage analysis; a run against `Light.PortableResults.AspNetCore.Shared` reports a non-zero mutation score, and a run against `Result.cs` reports zero `NoCoverage` mutants.
- [ ] Mutation output is contained under `StrykerOutput/`, which is ignored by git; a completed or interrupted run leaves `git status` clean.
- [ ] `tests/AGENTS.md` documents the file-scoped and project-scoped invocations, states that a surviving mutant is a signal to investigate together with the three permitted triage outcomes, records the measured cost per project, and records the mutation blind spots that must not be read as adequate coverage.
- [ ] Mutation testing runs on demand from a developer machine only. No CI workflow is added and no existing workflow is modified; pull request validation time is unchanged.
- [ ] The documented invocation is verified to run the intended tests: for a run mutating `AspNetCore.Shared`, Stryker's reported test count is 337 — every test project transitively referencing it — and not the 26 tests of `AspNetCore.Shared.Tests` alone.
- [ ] A baseline mutation score is recorded in `tests/AGENTS.md` for the four projects that complete in minutes (`AspNetCore.Shared`, `AspNetCore.Mvc`, `AspNetCore.MinimalApis`, `Validation.OpenApi`), together with the test count each run executed.
- [ ] Mutation score thresholds and `break-at` remain unset; nothing in the repository fails a build or a test run because of a mutation score.

## Technical Details

### Verified constraints

The findings below come from `dotnet-stryker` 4.16.0 executed against this solution. They are the reason the configuration is not left at its defaults.

Microsoft Testing Platform support is still a preview feature in Stryker.NET, and it announces itself as one on every run. It is tracked upstream in [stryker-mutator/stryker-net#3094](https://github.com/stryker-mutator/stryker-net/issues/3094), which is the issue to watch for the maturity of everything in this section.

**The default `vstest` runner is unusable here.** These test projects set `UseMicrosoftTestingPlatformRunner`, and VSTest cannot drive their xUnit v3 hosts ([#3117](https://github.com/stryker-mutator/stryker-net/issues/3117)). Coverage capture fails and every mutant is reported as survived — `AspNetCore.Shared` scores 0.00% under `vstest` and 100.00% under `mtp`. The failure is a logged error plus a plausible-looking report, not a crash, so `"test-runner": "mtp"` must be committed rather than passed ad hoc.

**MTP coverage analysis fabricates `NoCoverage`.** With the default `perTest` analysis, `Result.cs` reports 6 killed and 61 `NoCoverage` for a score of 8.96%, despite `NonGenericResultTests` covering those members directly. With `"coverage-analysis": "off"` the same file reports 67 tested, 0 uncovered, and 100.00%. A false `NoCoverage` is the most damaging possible output for the intended agent workflow, because it directs effort at tests that already exist. Per-test coverage for the MTP runner is in progress as [#3516](https://github.com/stryker-mutator/stryker-net/pull/3516); revisit this setting when it ships, since it is the main lever on run time.

The configuration is therefore:

```json
{
  "stryker-config": {
    "solution": "Light.PortableResults.slnx",
    "test-runner": "mtp",
    "coverage-analysis": "off",
    "reporters": ["json", "html", "progress"]
  }
}
```

Everything else about the repository is compatible without special handling: `.slnx` analysis, central package management with lock files, the `netstandard2.0;net10.0` multi-targeting (Stryker selects `net10.0` unaided), the netstandard2.0 source generator, `InterceptorsNamespaces`, and the Verify-based snapshot tests, which leave no `.received.*` files behind because 4.14.2 disables DiffEngine under MTP. No strong-naming or `InternalsVisibleTo` seam exists to work around.

Run mutation testing in the `Debug` configuration. `TreatWarningsAsErrors` is Release-only, and Stryker's rollback should not have to contend with warnings promoted to errors.

Reports are written to `StrykerOutput/<timestamp>/` beneath the working directory; the JSON and HTML reporters exist to produce files, so the goal is containment, not absence. Stryker already writes a `.gitignore` containing `*` into each timestamped directory, and a completed run leaves `git status` clean without any repository change. Add `StrykerOutput/` to `.gitignore` anyway: it states the intent, covers the directory itself, and protects against a run interrupted before that inner file is written. The output location is a CLI concern — `--output` has no `stryker-config.json` equivalent — so nothing here depends on redirecting it, and no wrapper script is needed.

### Cost

Measured throughput is approximately three seconds of wall clock per mutant at concurrency 8, dominated by fixed per-mutant overhead rather than test count: the 337-test and 2,398-test suites cost the same per mutant. Mutant inventory:

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

A solution-wide run is roughly seven hours on the measured hardware, which is why this stays a local, on-demand tool rather than anything automated. Mutation testing is not added to CI in any form: no new workflow, and no change to `build-and-test.yml`. Whoever drives it locally chooses the scope, and the practical scopes are one file or one project — never the whole solution in the inner loop.

The two large projects are long-running even in isolation: `Light.PortableResults` is roughly 3.6 hours and `Validation` roughly 1.8 hours. When one of them is worth running end to end, split it by `mutate` glob along folder boundaries (`Metadata/`, `Http/`, `CloudEvents/`, `Numbers/`) so the run is interruptible and each report arrives while it is still actionable.

The JSON report is the agent-facing artifact; the HTML report is for humans. Agents filter for `"status": "Survived"` to obtain a work queue located at specific files and lines. A survivor is a signal to investigate, not a defect to fix on sight; see triage below.

### Scoped invocation

Because the configuration names the solution, every run is a solution-context run. `-p` selects which source project is mutated; it does not narrow which tests execute. Stryker discovers the test projects from the solution and runs **every test project that transitively references the mutated project**. A `-tp` argument does not override this and must not be documented as if it did.

This was confirmed by test counts rather than inferred: mutating `AspNetCore.Shared` reports 337 tests, which is the exact sum of the six test projects referencing it (26 + 46 + 23 + 79 + 112 + 51), not the 26 in `AspNetCore.Shared.Tests`. Mutating `Light.PortableResults` reports 2,398 — the whole solution, since everything references it.

Keep this behavior rather than forcing a single test project. It matches the sociable-testing rule in `tests/AGENTS.md`: a mutant in `AspNetCore.Shared` killed by `MinimalApis.Tests` is legitimately killed, and isolating test projects would convert those cross-project kills into false survivors. It is also close to free, because per-mutant cost is dominated by fixed overhead — the 337-test and 2,398-test sets cost the same per mutant.

The form to document for agents is therefore source project plus optional file glob. Mutating one file takes about four minutes:

```shell
dotnet stryker -p <Source>.csproj -m '**/TheFile.cs'
```

Whole small projects (`AspNetCore.Shared`, `Mvc`, `MinimalApis`, `Validation.OpenApi`) complete in one to two minutes and need no `-m`. Omitting `-p` mutates every source project in the solution and is the seven-hour path.

Do not use or document `--since` under this configuration. It was measured on this branch and never narrowed anything: with a completely clean working tree it still reported `ai-plans/0063-add-stryker-mutation-testing.md` as a changed test file and escalated to `16 mutants will be tested because: Non-CSharp files in test project were changed`.

Two behaviors combine badly here. Stryker treats any non-C# file in the diff — including a Markdown plan — as a changed test file and responds by testing every mutant; and `--since:HEAD` did not pin the baseline to `HEAD`, so the diff kept resolving against the default branch. Because every feature branch in this repository begins by adding a plan under `ai-plans/`, a non-C# file is essentially always in the diff. `--since` therefore degrades to a full project run while presenting itself as scoped, which is worse than not using it: the cost is unchanged and the reported scope is wrong.

Explicit file and project scope is the only recommended form. Revisit `--since` only once MTP coverage analysis is trustworthy — a separate concern is that mapping changed *test* files back to mutants depends on per-mutant covering tests, which `coverage-analysis: off` does not produce, so a test-only edit has no reliable path to the mutants it affects.

### Triaging survivors

A surviving mutant means the suite did not distinguish the mutated program from the original. That has three admissible causes, and exactly three permitted responses:

1. **Observable behavior is genuinely unconstrained.** Add or strengthen a test. This is the outcome the tool exists to produce and should be the common one.
2. **The mutant is equivalent or invalid** — semantically identical to the original, or killable only by asserting on something that is not part of the contract. Suppress it narrowly at the source with a justification, which Stryker records in the report:

   ```csharp
   // Stryker disable once Statement : equivalent - the guard is a fast path, not a behavior change
   ```

   The syntax is `Stryker [disable|restore][once][all|<mutator list>][: reason]`, and it is scope-aware. Prefer `disable once` over `disable all`, and never reach for the global `ignore-mutations` setting to silence a single site.
3. **The mutant sits in a construct this configuration cannot meaningfully test.** Record it and move on; see the blind spots below.

Never restructure production code solely to make a mutant killable. The root `AGENTS.md` ranks performance above extensibility, and this library's low-allocation `in`/`ref`/`Span` style is precisely the shape that produces awkward survivors. A lower mutation score is the correct outcome when the alternative is a slower or less direct implementation.

No run performed while preparing this plan produced a single survivor — every scored run returned 100.00%. The first real baseline is therefore also the first exercise of this guidance, and the categories above are stated a priori rather than derived from survivors observed in this codebase.

### Blind spots to document

Roughly 9.5% of mutants fail to compile, concentrated in the low-allocation `out`/`ref` style. Stryker responds with Safe Mode, which discards every mutant in the enclosing method:

```
CS0165: Use of unassigned local variable 'low'  (Numbers/Dragon4.cs:263)
[INF] Safe Mode! Stryker will remove all mutations in GenerateDigits
```

`Dragon4.GenerateDigits`, `ResultJsonReader.ReadStatusValue`, `ResultJsonReader.ReadIndexValue`, `ErrorsExtensions.WriteRichErrors`, and the whole of `LightResult.cs` (11 of 11 mutants) receive no mutation coverage at all. This is a tool limitation, not a test defect. `tests/AGENTS.md` must state it explicitly so that a high mutation score in `Numbers/` is not mistaken for verified behavior, and so that no one attempts to "fix" it by restructuring production code.

Mutation score is a diagnostic here, not a gate. Leave the coverage threshold machinery in `build-and-test.yml` alone, and do not let a mutation score fail any automated check.
