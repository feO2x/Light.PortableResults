# 0022 Rename Summary

## Scope

This summary covers branch `22-rename-to-portable-results` compared to `main`.

## Change Size

- 315 files changed
- 1028 insertions, 1007 deletions
- Predominantly mechanical rename/refactor work, plus targeted API and documentation fixes.

## 1. Repository and Package Identity Renamed

The solution moved from **Light.Results** to **Light.PortableResults** across repository artifacts:

- Solution and signing artifacts renamed:
    - `Light.Results.slnx` -> `Light.PortableResults.slnx`
    - `Light.Results.Public.snk` -> `Light.PortableResults.Public.snk`
    - dotsettings file renamed accordingly
- Repository URLs and package metadata updated to `Light.PortableResults`
- Package names in README and project descriptions updated to the new identity

## 2. Project and Folder Renames Across `src/` and `tests/`

All major project directories and `.csproj` names were renamed from `Light.Results*` to `Light.PortableResults*`,
including:

- Core library
- ASP.NET Core Shared/Minimal APIs/MVC integration projects
- All corresponding test projects
- Benchmarks project references

This includes namespace updates and file renames for consistency.

## 3. Public API Naming Updates (PortableResults Prefix)

Key API names were modernized to match the new package identity.

### Core option type renames

- `LightResultsHttpWriteOptions` -> `PortableResultsHttpWriteOptions`
- `LightResultsHttpReadOptions` -> `PortableResultsHttpReadOptions`
- `LightResultsCloudEventsWriteOptions` -> `PortableResultsCloudEventsWriteOptions`
- `LightResultsCloudEventsReadOptions` -> `PortableResultsCloudEventsReadOptions`

### ASP.NET Core integration surface

- `AddLightResultsForMinimalApis` -> `AddPortableResultsForMinimalApis`
- `AddLightResultsForMvc` -> `AddPortableResultsForMvc`
- `LightResultEndpointExtensions` -> `PortableResultsEndpointExtensions`
- `ProducesLightResultAttribute` -> `ProducesPortableResultAttribute`
- `LightResultsMinimalApiJsonContext` -> `PortableResultsMinimalApiJsonContext`
- `ResolveLightResultsHttpWriteOptions` -> `ResolvePortableResultsHttpWriteOptions`

### Module registration/API naming updates

- HTTP writing module renamed APIs to `PortableResults*` variants (including JSON converter registration and header
  conversion service registration)
- HTTP reading module renamed APIs to `PortableResults*` variants
- CloudEvents reading module renamed APIs to `PortableResults*` variants
- CloudEvents writing options type renamed to `PortableResultsCloudEventsWriteOptions`

## 4. CloudEvents Contract Update: `lroutcome` -> `lproutcome`

The reserved Light extension attribute was fully renamed:

- Wire attribute key:
    - `lroutcome` -> `lproutcome`
- Constant renamed:
    - `CloudEventsConstants.LightResultsOutcomeAttributeName` ->
      `CloudEventsConstants.PortableResultsOutcomeAttributeName`

Updated areas:

- CloudEvents writer (`JsonCloudEventsExtensions`)
- CloudEvents reader (`ReadOnlyMemoryCloudEventsExtensions`)
- Reserved/forbidden extension filtering logic
- Error/help messages that reference the reserved attribute
- CloudEvents unit tests
- CloudEvents benchmarks
- Internal planning docs that documented the old key

No backward-compatibility path for `lroutcome` was kept.

## 5. README and Documentation Alignment

Root `README.md` was brought in line with the renamed codebase and APIs:

- Package names and links updated
- `using` namespaces corrected
- DI registration samples updated to current method names
- Option type names in configuration snippets updated
- CloudEvents source URN examples updated to `light-portable-results`
- Sample code terminology aligned with current API surface

Additional docs updated:

- `AGENTS.md`, `src/AGENTS.md`, `tests/AGENTS.md`
- Selected `ai-plans` documents reflecting renamed CloudEvents key and product name

## 6. CI/CD and Build Pipeline Adjustments

GitHub workflows were updated to new solution/signing names:

- Build/test workflow now restores/builds/tests `Light.PortableResults.slnx`
- NuGet release workflow now packs with `Light.PortableResults.slnx` and `Light.PortableResults.snk`
- Release workflow SNK handling was hardened with shell safety flags and cleanup trap

## 7. Tests and Benchmarks Updated to Match New Names

- Test project names, namespaces, file paths, and snapshots were renamed to `Light.PortableResults*`
- Assertions and JSON payload fixtures were updated to reflect renamed APIs and `lproutcome`
- Benchmark code updated to reference renamed projects/types and CloudEvents outcome key

## Outcome

The branch transitions the codebase from **Light.Results** to **Light.PortableResults** end-to-end, including package
identity, source/test project structure, public API naming, documentation, CI workflow references, and CloudEvents
outcome extension naming.
