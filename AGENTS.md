# Root Agents.md

Light.Results is a lightweight, high-performance library implementing the Result Pattern for .NET. It stands out for reducing allocations and being able to serialize and deserialize results across different protocols (HTTP via RFC-9457, gRPC, Asynchronous Messaging). Extensibility is less important than performance.

## General Rules for the Code Base

In our Directory.Build.props files in this solution, the following rules are defined:

- Implicit usings or global usings are not allowed - use explicit using statements for clarity.
- Light.Results project is built with .NET Standard 2.0, but you can use C# 14 features.
- All other projects use .NET 10, including the test projects.
- The library is not published yet, you can make breaking changes.
- `<TreatWarningsAsErrors>` is enabled in Release builds, so your code changes must not generate warnings.
- If it is properly encapsulated, make it public. We don't know how callers would like to use this library. When some
  types are internal, this might make it hard for callers to access these in tests or when making configuration changes.
  Prefer public APIs over internal ones.

## Production Code Rules

Read ./src/AGENTS.md for details about the production code.

## Testing Rules

Read ./tests/AGENTS.md for details about how to write tests.

## Plan Rules

Read ./ai-plans/AGENTS.md for details on how to write plans.

## Here is Your Space

If you encounter something worth noting while you are working on this code base, write it down here in this section.
Once you are finished, I will discuss it with you and we can decide where to put your notes.
