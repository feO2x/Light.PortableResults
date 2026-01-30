# Root Agents.md

Light.Results is a lightweight, high-performance library implementing the Result Pattern for .NET. It stands out for reducing allocations and being able to serialize and deserialize results across different protocols (HTTP via RFC-9457, gRPC, Asynchronous Messaging). Extensibility is less important than performance.

## General Rules

In our Directory.Build.props files in this solution, the following rules are defined:

- Implicit usings or global usings are not allowed - use explicit using statements for clarity.
- Light.Results project is built with .NET Standard 2.0, but you can use C# 14 features.
- All other projects use .NET 10, including the test projects.
- The library is not published yet, you can make breaking changes.
- `<TreatWarningsAsErrors>` is enabled in Release builds, so your code changes must not generate warnings.
