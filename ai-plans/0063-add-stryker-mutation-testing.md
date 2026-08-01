# Stryker.NET Mutation Testing

## Rationale

Line coverage is held above 95%, but it only proves that code executed, not that any assertion constrains it. Coding agents extending this library cannot currently distinguish a well-tested code path from one that is merely reached. Mutation testing closes that gap: each surviving mutant is a concrete, machine-readable statement that some behavior can be changed without a single test noticing.

Introduce Stryker.NET as a pinned, opt-in tool with a repository configuration that is correct by default. Two settings are load-bearing — the wrong test runner and the default coverage analysis each produce confidently wrong reports on this solution — so the configuration, not the invocation, must carry them.

## Acceptance Criteria

- [ ] `dotnet tool restore` installs a version-pinned `dotnet-stryker`, and a repository-root `stryker-config.json` supplies the shared configuration so that a scoped run needs only project and test-project arguments.
- [ ] The committed configuration selects the Microsoft Testing Platform runner and disables coverage analysis; a run against `Light.PortableResults.AspNetCore.Shared` reports a non-zero mutation score, and a run against `Result.cs` reports zero `NoCoverage` mutants.
- [ ] `StrykerOutput/` is ignored by git, and no mutation run leaves artifacts inside the working tree.
- [ ] `tests/AGENTS.md` documents the file-scoped and project-scoped invocations, states that surviving mutants are the signal to act on, records the measured cost per project, and records the mutation blind spots that must not be read as adequate coverage.
- [ ] Mutation testing runs on demand from a developer machine only. No CI workflow is added and no existing workflow is modified; pull request validation time is unchanged.
- [ ] A baseline mutation score is recorded in `tests/AGENTS.md` for the four projects that complete in minutes (`AspNetCore.Shared`, `AspNetCore.Mvc`, `AspNetCore.MinimalApis`, `Validation.OpenApi`).
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

The JSON report is the agent-facing artifact; the HTML report is for humans. Agents filter for `"status": "Survived"`, and each survivor identifies a missing assertion at a specific file and line.

### Scoped invocation

The workflow that actually serves the goal is per-file, not per-solution. Mutating one source file against its test project takes about four minutes and is the form to document for agents:

```shell
dotnet stryker -p <Source>.csproj \
  -tp tests/<Source>.Tests/<Source>.Tests.csproj \
  -m '**/TheFile.cs'
```

`-p` resolves against the test project's references, so both arguments are required. Whole small projects (`AspNetCore.Shared`, `Mvc`, `MinimalApis`, `Validation.OpenApi`) complete in one to two minutes and need no `-m`.

`--since:<committish>` restricts mutation to files changed against a baseline and is the natural scope for "mutation-test what I just wrote" — for example `--since:main` while working on a feature branch. It reads git history directly, so it needs no special setup locally. Document it alongside the two forms above; it is the most convenient entry point for an agent that has just finished editing.

### Blind spots to document

Roughly 9.5% of mutants fail to compile, concentrated in the low-allocation `out`/`ref` style. Stryker responds with Safe Mode, which discards every mutant in the enclosing method:

```
CS0165: Use of unassigned local variable 'low'  (Numbers/Dragon4.cs:263)
[INF] Safe Mode! Stryker will remove all mutations in GenerateDigits
```

`Dragon4.GenerateDigits`, `ResultJsonReader.ReadStatusValue`, `ResultJsonReader.ReadIndexValue`, `ErrorsExtensions.WriteRichErrors`, and the whole of `LightResult.cs` (11 of 11 mutants) receive no mutation coverage at all. This is a tool limitation, not a test defect. `tests/AGENTS.md` must state it explicitly so that a high mutation score in `Numbers/` is not mistaken for verified behavior, and so that no one attempts to "fix" it by restructuring production code.

Mutation score is a diagnostic here, not a gate. Leave the coverage threshold machinery in `build-and-test.yml` alone, and do not let a mutation score fail any automated check.
