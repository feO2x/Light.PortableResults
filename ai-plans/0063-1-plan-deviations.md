# 0063 Plan Deviations

This document compares `ai-plans/0063-add-stryker-mutation-testing.md` with the results measured while implementing it (`dotnet-stryker` 4.16.0, Apple M3 Max, 16 logical cores, source tree at commit `04aee20`).

## Summary

All acceptance criteria were implemented and verified as specified: the pinned tool, the committed configuration, the scoped invocations, the transitive test counts, the git-ignored output containment, the recorded baseline, and the absence of any CI or threshold changes.

The material deviation is in the plan's Technical Details narrative about survivors: the claim of exactly one survivor across the four small projects did not reproduce. The survivor population in `Validation.OpenApi` is roughly twenty mutants, and most of it is hidden by timeout classification to a degree that varies with machine load and concurrency. The operational documentation in `tests/AGENTS.md` already reflects the measured behavior; this file records the delta from the plan text.

## Deviations From The Original Plan

### 1. `Validation.OpenApi` has ~18–20 survivors masked as timeouts, not one survivor

**Original plan:**
The plan states that the measured runs produced exactly one survivor across all four small projects — a statement-removal mutant at `BuiltInValidationErrorBuilderExtensions.cs:426`, putting `Validation.OpenApi` at 98.68% — and that the triage categories therefore "remain largely a priori rather than derived from survivors observed in this codebase."

**Measured:**
Three runs of the same unmodified sources produced three different survivor sets:

- Concurrency 8: 54 killed, 21 timeout, 1 survived — 98.68%. The survivor was an equality mutant at `BuiltInValidationErrorBuilderExtensions.cs:388`, not the statement removal at line 426.
- Concurrency 8, repeat: 51 killed, 21 timeout, 4 survived — 94.74%.
- Concurrency 4: 56 killed, 2 timeout, 18 survived — 76.32%.

The survivors cluster in `BuiltInValidationErrorBuilderExtensions.cs` lines 367–426 (conditional, equality, object-initializer, and statement mutants around the built-in validation error OpenAPI schema customization), with a few more in `BuiltInValidationErrorContracts.cs`, `BuiltInValidationErrorContractRegistrationExtensions.cs`, and `PortableValidationOpenApiRouteHandlerBuilderExtensions.cs`.

**Why:**
With `coverage-analysis: off`, every mutant runs the full discovered test set (163 tests for this project). A killed mutant's run aborts at the first failing test, but a genuinely surviving mutant runs the entire suite — and can exceed the per-mutant timeout. `Timeout` counts as killed in the mutation score, so under higher concurrency (more host contention) slow survivors are reclassified as timeouts and the score is inflated. The plan's "one survivor, 98.68%" is one point in this timing-dependent distribution, not a stable property of the test suite.

**Impact:**
The initial triage queue is ~18–20 mutants in that file, not one, and the triage categories are no longer a priori — they now have real input. No acceptance criterion is affected: criterion 7 deliberately requires recording the timeout count alongside the others, and the baseline in `tests/AGENTS.md` carries the measured vector. The blind-spots section there directs agents to treat `Timeout` as a triage signal equal to `Survived` for this project. Future comparisons should match full count vectors at equal concurrency rather than percentages.

### 2. The recorded baseline's provenance commit predates the tooling commit

**Original plan:**
Criterion 7 requires the baseline to carry the commit at which it was measured.

**Measured:**
The baseline was measured at commit `04aee20` while the implementation itself (tool manifest, `stryker-config.json`, `.gitignore`) was still uncommitted. The mutated sources were identical to `04aee20`, so mutation results are unaffected, but once this work is committed the provenance commit recorded in `tests/AGENTS.md` is an ancestor of the tooling commit rather than the commit that introduced the tool.

**Impact:**
Cosmetic. Re-measuring the baseline at the merge commit is a valid cheap follow-up (about six minutes for all four projects) if exact provenance is ever needed.

### 3. The committed configuration pins `concurrency: 8`, which the plan did not specify

**Original plan:**
The plan specifies the exact contents of `stryker-config.json` (`solution`, `test-runner`, `coverage-analysis`, `configuration`, `reporters`) and requires that a scoped run needs only a `-p` argument and an optional `-m` glob. Concurrency appears only as baseline provenance, supplied via `-c 8` at invocation time — so the documented invocation was not the invocation that produced the baseline.

**Implemented:**
`stryker-config.json` additionally pins `"concurrency": 8`, and the documented invocations pass no `-c`.

**Why:**
The result vector is concurrency-sensitive (§1: `Validation.OpenApi` reports 2 timeouts/18 survivors at concurrency 4 vs. 21/1 at 8), so a baseline is only comparable at equal concurrency — making concurrency a load-bearing setting, which the plan's own philosophy places in the configuration rather than the invocation. Relying on the default is not an alternative: 4.16.0 defaults to half the logical processors (verified via the debug options dump; the `--help` text claiming "as many parallel processes as you have CPU cores" is stale), which is 8 on the recording machine but 4 on an 8-core machine. The pinned value reproduces the baseline on any machine with at least 8 logical cores and leaves `-c` free for experiments, e.g. lowering it to unmask slow survivors. Verified to take effect: with `"concurrency": 3` in the config and no `-c`, the debug options dump reports `"Concurrency": 3`.

## Notes On Items Implemented As Planned

The following measurements from the plan reproduced exactly or within seconds:

- Mutant inventories and test counts for all four small projects: 33/32/44/114 mutants, 11/11/3/2 compile errors, 102/237/337/163 tests (`AspNetCore.Mvc`, `AspNetCore.MinimalApis`, `AspNetCore.Shared`, `Validation.OpenApi`).
- `AspNetCore.Shared` reporting 337 tests (all six transitively referencing test projects) and `Result.cs` reporting 2,398 tests with 67 killed, 0 `NoCoverage`, 100.00%.
- Elapsed times: 0:40 / 0:58 / 1:57 / 2:06 measured vs. 0:38 / 0:57 / 1:57 / 1:59 in the plan.
- `AspNetCore.Mvc`, `AspNetCore.MinimalApis`, and `AspNetCore.Shared` each scoring 100.00%.
- `StrykerOutput/` containment (eight runs, no `git status` entries), no CI changes, and no `thresholds`/`break-at` settings anywhere.

## Minor Operational Notes

- `dotnet stryker --version` is not a tool-version query — it is Stryker's `--version <value>` option for the analyzed project and errors with "Missing value for option 'version'". Use `dotnet tool list` to confirm the pinned tool version.
