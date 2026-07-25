# Root Agents.md

Light.PortableResults is a lightweight, high-performance library implementing the Result Pattern for .NET. It stands out for reducing allocations and being able to serialize and deserialize results across different protocols (HTTP via RFC-9457, gRPC, Asynchronous Messaging). Extensibility is less important than performance.

## Implementation rules

Plans typically have acceptance criteria with check boxes. Check each box when you are finished with the corresponding criterion.

## General Rules for the Code Base

In our Directory.Build.props files in this solution, the following rules are defined:

- Implicit usings or global usings are not allowed - use explicit using statements for clarity.
- The Light.PortableResults project is built with .NET Standard 2.0, but you can use C# 14 features.
- All other projects use .NET 10, including the test projects.
- The library is not published in a stable version yet, you can make breaking changes.
- `<TreatWarningsAsErrors>` is enabled in Release builds, so your code changes must not generate warnings.
- When a type or method is properly encapsulated, make it public. We don't know how callers would like to use this library. When some types are internal, this might make it hard for callers to access these in tests or when making configuration changes. Prefer public APIs over internal ones.

## Testing Rules

Read ./tests/AGENTS.md for details about how to write tests.

## Plan Rules

Read ./ai-plans/AGENTS.md for details on how to write plans.

## Here is Your Space

If you encounter something worth noting while you are working on this code base, write it down here in this section. Once you are finished, I will discuss it with you, and we can decide where to put your notes.

### Notes from plan 0052 (decimal metadata kind)

- Solution-wide line coverage sits right on the 95% threshold (94.8% before this plan, 95.0% after). The margin comes from `Light.PortableResults.AspNetCore.MinimalApis` (87.6%), `Light.PortableResults.AspNetCore.Mvc` (87.9%), and `Light.PortableResults.Validation.OpenApi.SourceGeneration` (87%), while the core package is at 96.2%. Any future plan whose acceptance criteria include the 95% gate will be paying off that debt rather than covering its own code. Worth deciding whether the gate should be per-assembly.
- `MetadataValueTestFactory` (in `Light.PortableResults.Tests/Metadata`) builds a `MetadataValue` with an undeclared `MetadataKind` via reflection on the private constructor. It is the only way to reach the fallback arms of the kind dispatch sites. If the constructor signature changes, that helper and `UndeclaredMetadataKindFallbackTests` break.
