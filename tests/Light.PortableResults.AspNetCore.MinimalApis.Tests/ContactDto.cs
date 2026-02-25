using System;

namespace Light.PortableResults.AspNetCore.MinimalApis.Tests;

public sealed record ContactDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
}
