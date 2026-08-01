# Root Agents.md

Light.PortableResults is a lightweight, high-performance library implementing the Result Pattern for .NET. It stands out for reducing allocations and being able to serialize and deserialize results across different protocols (HTTP via RFC-9457, gRPC, Asynchronous Messaging). Extensibility is less important than performance.

## Implementation rules

Plans typically have acceptance criteria with check boxes. Check each box when you are finished with the corresponding criterion.

## General Rules for the Code Base

In our Directory.Build.props files in this solution, the following rules are defined:

- Implicit usings or global usings are not allowed - use explicit using statements for clarity.
- Use C# 14 across all projects.
- The library is not published in a stable version yet, you can make breaking changes.
- `<TreatWarningsAsErrors>` is enabled in Release builds, so your code changes must not generate warnings.
- When a type or method is properly encapsulated, make it public. We don't know how callers would like to use this library. When some types are internal, this might make it hard for callers to access these in tests or when making configuration changes. Prefer public APIs over internal ones.
- Use Conventional Commits messages. Decide whether a commit title is enough or a commit body is required.

## Testing Rules

Read ./tests/AGENTS.md for details about how to write tests.

## Plan Rules

Read ./ai-plans/AGENTS.md for details on how to write plans.

## Here is Your Space

If you encounter something worth noting while you are working on this code base, write it down here in this section. Once you are finished, I will discuss it with you, and we can decide where to put your notes.
