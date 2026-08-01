# 0063 Plan Deviations

This document compares `ai-plans/0063-0-add-stryker-mutation-testing.md` with the results measured while implementing it (`dotnet-stryker` 4.16.0, Apple M3 Max, 16 logical cores, source tree at commit `04aee20`).

## Summary

All acceptance criteria were implemented and verified as specified: the pinned tool, the committed configuration, the scoped invocations, the transitive test counts, the git-ignored output containment, the recorded baseline, and the absence of any CI or threshold changes.

The material deviations are in the plan's Technical Details narrative about survivors and in two settings added to make the measured baseline meaningful. The claim of exactly one survivor across the four small projects did not reproduce: `Validation.OpenApi` has nineteen survivors, and the original 5,000 ms additional-timeout default hid most of them as timeouts. The committed configuration pins concurrency and raises the additional timeout so the operational report exposes that queue directly. The operational documentation in `tests/AGENTS.md` reflects the measured behavior; this file records the delta from the plan text.

## Deviations From The Original Plan

### 1. `Validation.OpenApi` has nineteen survivors, not one

**Original plan:**
The plan states that the measured runs produced exactly one survivor across all four small projects — a statement-removal mutant at `BuiltInValidationErrorBuilderExtensions.cs:426`, putting `Validation.OpenApi` at 98.68% — and that the triage categories therefore "remain largely a priori rather than derived from survivors observed in this codebase."

**Measured:**
With Stryker's 5,000 ms default additional timeout, three runs of the same unmodified sources produced three different survivor sets:

- Concurrency 8: 54 killed, 21 timeout, 1 survived — 98.68%. The survivor was an equality mutant at `BuiltInValidationErrorBuilderExtensions.cs:388`, not the statement removal at line 426.
- Concurrency 8, repeat: 51 killed, 21 timeout, 4 survived — 94.74%.
- Concurrency 4: 56 killed, 2 timeout, 18 survived — 76.32%.

Increasing the additional timeout exposed the stable population much more directly at concurrency 8:

- 15,000 ms: 57 killed, 1 timeout, 18 survived — 76.32%, 2:30 elapsed.
- 20,000 ms: two runs both reported 57 killed, with 0–1 timeout and 18–19 survived — 75.00–76.32%, 2:34–2:38 elapsed. The mutant that timed out in the second run survived in the first.
- 30,000 ms: two consecutive runs both reported 57 killed, 0 timeout, and 19 survived — 75.00%, 2:32–2:38 elapsed.

The survivors cluster in `BuiltInValidationErrorBuilderExtensions.cs` lines 367–426 (conditional, equality, object-initializer, and statement mutants around the built-in validation error OpenAPI schema customization), with a few more in `BuiltInValidationErrorContracts.cs`, `BuiltInValidationErrorContractRegistrationExtensions.cs`, and `PortableValidationOpenApiRouteHandlerBuilderExtensions.cs`.

**Why:**
With `coverage-analysis: off`, every mutant runs the full discovered test set (163 tests for this project). A killed mutant's run aborts at the first failing test, but a genuinely surviving mutant runs the entire suite and can exceed the timeout under load. Stryker 4.16.0 calculates that timeout from the measured initial run plus `additional-timeout`, which defaults to 5,000 ms and is configurable only in the config file. `Timeout` counts as killed in the mutation score, so insufficient headroom reclassifies slow survivors as timeouts and inflates the score. The plan's "one survivor, 98.68%" is one point in this timing-dependent distribution, not a stable property of the suite.

**Impact:**
The initial triage queue is nineteen mutants, not one, and the triage categories are no longer a priori — they now have real input. The 30,000 ms committed setting reduces the timeout count from 21 to zero on the measured hardware without changing the mutant inventory. No acceptance criterion is affected: criterion 7 deliberately requires recording the timeout count alongside the others, and the baseline in `tests/AGENTS.md` carries the measured vector. Future timeouts remain triage signals because no finite headroom can make classification independent of hardware and load. Future comparisons should match full count vectors at equal concurrency and additional timeout rather than percentages.

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
The result vector is concurrency-sensitive (§1: `Validation.OpenApi` reports 2 timeouts/18 survivors at concurrency 4 vs. 21/1 at 8), so a baseline is only comparable at equal concurrency — making concurrency a load-bearing setting, which the plan's own philosophy places in the configuration rather than the invocation. Relying on the default is not an alternative: 4.16.0 defaults to half the logical processors (verified via the debug options dump; the `--help` text claiming "as many parallel processes as you have CPU cores" is stale), which is 8 on the recording machine but 4 on an 8-core machine. The pinned value ensures the same configured parallelism on machines with at least 8 logical cores, but does not make timing independent of hardware or load; it also leaves `-c` free for experiments. Verified to take effect: with `"concurrency": 3` in the config and no `-c`, the debug options dump reports `"Concurrency": 3`.

### 4. The committed configuration pins `additional-timeout: 30000`, which the plan did not specify

**Original plan:**
The plan leaves Stryker's additional timeout unset, so 4.16.0 uses its 5,000 ms default. It treats timeout masking as a blind spot to document rather than identifying the setting that controls it.

**Implemented:**
`stryker-config.json` additionally pins `"additional-timeout": 30000`. The option is milliseconds of headroom added to Stryker's timeout derived from the initial test run; it is config-file-only in 4.16.0 and absent from `--help`.

**Why:**
The default systematically hid slow survivors under `coverage-analysis: off`. At concurrency 8, 15,000 ms reduced `Validation.OpenApi` from 21 timeouts to one but still timed out a mutant known to survive, and one of two 20,000 ms runs did the same. Two consecutive 30,000 ms runs reported zero timeouts and exposed all 19 survivors. The higher setting also reclassified `AspNetCore.Mvc`'s former timeout at `BaseLightActionResult.cs:82` as a survivor. It does not make the result hardware-independent, but it removes all known masking on the measured hardware without changing concurrency or requiring readers to reinterpret dozens of timeouts.

**Impact:**
The four small projects were remeasured under the committed setting and the baseline in `tests/AGENTS.md` was replaced. Genuine hangs can now take up to 25 seconds longer per affected test assembly than under the default, while ordinary killed mutants still stop at the first failing test. The measured small-project runs remain within minutes. A future timeout is still a triage signal, not automatically a killed mutant.

## Notes On Items Implemented As Planned

The following measurements remained unchanged after the timeout correction:

- Mutant inventories and test counts for all four small projects: 33/32/44/114 mutants, 11/11/3/2 compile errors, 102/237/337/163 tests (`AspNetCore.Mvc`, `AspNetCore.MinimalApis`, `AspNetCore.Shared`, `Validation.OpenApi`).
- `AspNetCore.Shared` reporting 337 tests (all six transitively referencing test projects) and `Result.cs` reporting 2,398 tests with 67 killed, 0 `NoCoverage`, 100.00%.
- `AspNetCore.MinimalApis` and `AspNetCore.Shared` scoring 100.00%. `AspNetCore.Mvc` now scores 94.12% because its former timeout is correctly reported as survived.
- `StrykerOutput/` containment across repeated runs, no CI changes, and no `thresholds`/`break-at` settings anywhere.

## Minor Operational Notes

- `dotnet stryker --version` is not a tool-version query — it is Stryker's `--version <value>` option for the analyzed project and errors with "Missing value for option 'version'". Use `dotnet tool list` to confirm the pinned tool version.
