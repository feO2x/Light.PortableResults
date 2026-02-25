using System;

namespace Light.PortableResults.AspNetCore.Mvc.Tests;

public sealed record ContactDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
}
