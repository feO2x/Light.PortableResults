namespace Light.PortableResults.Tests.SourceGeneratedSerialization;

/// <summary>
/// The consumer-owned result value type used by the source-generation tests. It is the only type the test contexts
/// declare - every Light.PortableResults envelope and payload type must be resolved by the library itself.
/// </summary>
public sealed record MovieDto(string Title, int Year);
