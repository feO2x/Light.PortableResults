using System;

namespace Light.Results.AspNetCore.Mvc.Tests;

public sealed record ContactDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
}
