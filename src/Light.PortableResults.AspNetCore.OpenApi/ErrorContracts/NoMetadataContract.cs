namespace Light.PortableResults.AspNetCore.OpenApi.ErrorContracts;

/// <summary>
/// Represents a metadata contract for error codes that do not emit metadata.
/// </summary>
public sealed class NoMetadataContract : ErrorMetadataContract
{
    internal NoMetadataContract() { }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is NoMetadataContract;

    /// <inheritdoc />
    public override int GetHashCode() => 0;
}
