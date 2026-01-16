using Light.Results.AspNetCore.Shared;

namespace Light.Results.AspNetCore.MinimalApis.ResultsConversion;

public record ResultFactoryOptions
{
    public bool FirstCategoryIsLeadingCategory { get; init; } = false;

    public ErrorSerializationFormat ErrorSerializationFormat { get; init; } =
        ErrorSerializationFormat.AspNetCoreCompatible;
}
